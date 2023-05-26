using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PvInstallationContext>(options => options.UseSqlServer(builder.Configuration["ConnectionStrings:DefaultConnection"]));
var app = builder.Build();

List<PvInstallation> installations = new List<PvInstallation>();

app.MapPost("/installations", async (HttpRequest request, PvInstallationContext context, PvInstallationDto dto) =>{
    var PvInstallation = await context.PvInstallations.AddAsync(new PvInstallation
    {
        Longitude = dto.Longitude,
        Latitude = dto.Latitude,
        Address = dto.Address,
        OwnerName = dto.OwnerName,
        isActive = true,
        Comments = dto.Comments
    });
    await context.SaveChangesAsync();
    return Results.Ok(PvInstallation);
});

app.MapPost("/installations/{id}/deactivate", (int id) =>{
    var installation = installations.FirstOrDefault(i => i.ID == id);
    if(installation != null){
        installation.isActive = false;
        return Results.Ok(installation.ID);
    }
    
    return Results.NotFound();
});

app.MapPost("/installations/{id}/reports", async (HttpRequest request, PvInstallationContext context, ProductionReportDto dto, int id) =>{
    var ProductionReport = await context.ProductionReports.AddAsync(new ProductionReport{
        Timestamp = System.DateTime.UtcNow,
        ProducedWattage = dto.ProducedWattage,
        HouseholdWattage = dto.HouseholdWattage,
        GridWattage = dto.GridWattage,
        BatteryWattage = dto.BatteryWattage,
        PvInstallationID = id
    }); 
    
    return Results.Ok(ProductionReport);
});

app.MapGet("/installations/{id}/reports", (DateTime startTimeStamp, int duration, int id, PvInstallationContext context) => {
    DateTime endTimeStamp = startTimeStamp.AddMinutes(duration);

    List<ProductionReport> productionReports = new List<ProductionReport>();

    var filteredInstallations = productionReports.Where(i =>i.ID == id && i.Timestamp >= startTimeStamp && i.Timestamp < endTimeStamp);
    
    float sum = filteredInstallations.Sum(i => i.ProducedWattage);
    return Results.Ok(sum);
});

app.Run();

record PvInstallationDto(int InstallationID, string Longitude, string Latitude, string Address, string OwnerName, bool isActive, string? Comments);
record ProductionReportDto(int ReportID, DateTime Timestamp, float ProducedWattage, float HouseholdWattage, float BatteryWattage, float GridWattage);

class PvInstallation{
    public int ID {get;set;}
    public string Longitude {get;set;} = "";
    public string Latitude {get; set;} = "";
    public string Address {get;set;} = "";
    public string OwnerName {get;set;} = "";
    public bool isActive {get;set;}
    public string Comments {get;set;} = "";
    public List<ProductionReport> productionReports {get;set;} = new(); 
}

class ProductionReport{
    public int ID {get; set;}
    public DateTime Timestamp {get;set;}
    public float ProducedWattage {get;set;}
    public float HouseholdWattage {get;set;}
    public float BatteryWattage {get;set;}
    public float GridWattage {get;set;} 
    public int PvInstallationID {get;set;}
    public PvInstallation? PvInstallation {get;set;}
}

class PvInstallationContext : DbContext {
    public PvInstallationContext(DbContextOptions<PvInstallationContext> options) :base(options){}

    public DbSet<PvInstallation> PvInstallations => Set<PvInstallation>();
    public DbSet<ProductionReport> ProductionReports => Set<ProductionReport>();
}