using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpsTrack.Business.WorkRequests;
using OpsTrack.Data.Entities;

namespace OpsTrack.Api.Tests;

[Trait("Area", "WorkRequest")]
public class WorkRequestsApiTests : IClassFixture<OpsTrackApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly OpsTrackApiFactory _factory;

    public WorkRequestsApiTests(OpsTrackApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient ClientAs(string user, string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User", user);
        client.DefaultRequestHeaders.Add("X-User-Role", role);
        return client;
    }

    [Fact]
    public async Task List_returns_the_seeded_work_requests()
    {
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<PagedResult<WorkRequestDto>>("/api/work-requests?pageSize=100", Json);

        Assert.NotNull(page);
        Assert.True(page.Total >= 30, $"expected at least 30 seeded rows, got {page.Total}");
        Assert.Contains(page.Items, w => w.Number == "WR-2611-0022");
    }

    [Fact]
    public async Task Get_by_id_returns_the_work_request()
    {
        var client = _factory.CreateClient();
        var page = await client.GetFromJsonAsync<PagedResult<WorkRequestDto>>("/api/work-requests?pageSize=1", Json);
        var first = page.Items[0];

        var item = await client.GetFromJsonAsync<WorkRequestDto>($"/api/work-requests/{first.Id}", Json);

        Assert.Equal(first.Number, item.Number);
        Assert.Equal(first.EquipmentCode, item.EquipmentCode);
    }

    [Fact]
    public async Task Create_with_known_equipment_returns_201_and_a_running_number()
    {
        var client = ClientAs("op.test", "Operator");

        var response = await client.PostAsJsonAsync("/api/work-requests", new
        {
            equipmentCode = "SMT2-RF-01",
            title = "ทดสอบสร้างงานซ่อม",
            description = "สร้างจาก integration test",
            priority = "P2",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<WorkRequestDto>(Json);
        Assert.Matches(@"^WR-2611-\d{4}$", created.Number);
        Assert.Equal(WorkRequestStatus.Open, created.Status);
        Assert.Equal("op.test", created.ReportedBy);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Closing_a_request_that_missed_its_SLA_calls_the_webhook_stub()
    {
        // WR-2611-0014 is a P1 created 2026-11-11 02:50; the fake clock says 2026-11-13 10:00, so it is late.
        var reader = _factory.CreateClient();
        var page = await reader.GetFromJsonAsync<PagedResult<WorkRequestDto>>("/api/work-requests?pageSize=100", Json);
        var late = page.Items.Single(w => w.Number == "WR-2611-0014");
        var before = _factory.Notifications.Requests.Count;

        var response = await ClientAs("tech.anan", "Technician").PostAsync($"/api/work-requests/{late.Id}/close", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sent = _factory.Notifications.Requests.Skip(before).ToList();
        Assert.Single(sent);
        Assert.Equal(OpsTrackApiFactory.TestWebhookUrl, sent[0].Uri.ToString());
        Assert.Contains("WR-2611-0014", sent[0].Body);
    }
}
