var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env");

var db = builder
    .AddMySql("db")
    .WithDataVolume("temasek-data")
    .WithPhpMyAdmin()
    .AddDatabase("temasek");

var clerkSecretKey = builder.AddParameter("clerk-secret-key", true);
var clerkPublishableKey = builder.AddParameter("clerk-publishable-key");
var clerkAuthorizedParties = builder.AddParameter("clerk-authorized-parties");
var formSgSecretKey = builder.AddParameter("formsg-secret-key", secret: true);
var formSgCallbackApiKey = builder.AddParameter("formsg-callback-api-key", secret: true);

var facilitiesEnabled = builder.AddParameter("facilities-enabled", "true");
var facilitiesServiceAccountJsonCredential = builder.AddParameter(
    "facilities-service-account-json-credential",
    secret: true
);
var facilitiesCalendarId = builder.AddParameter("facilities-calendar-id");
var facilitiesCarbonCopyCalendarId = builder.AddParameter("facilities-carbon-copy-calendar-id");

var api = builder
    .AddProject<Projects.Temasek_WebApi>("temasek-webapi")
    .WithReference(db)
    .WaitFor(db)
    .WithEnvironment("FormSg:CallbackApiKey", formSgCallbackApiKey)
    .WithEnvironment("FormSg:SecretKey", formSgSecretKey)
    .WithEnvironment("Clerk:SecretKey", clerkSecretKey)
    .WithEnvironment("Clerk:AuthorizedParties", clerkAuthorizedParties)
    .WithEnvironment("Facilities:Enabled", facilitiesEnabled)
    .WithEnvironment(
        "Facilities:ServiceAccountJsonCredential",
        facilitiesServiceAccountJsonCredential
    )
    .WithEnvironment("Facilities:CalendarId", facilitiesCalendarId)
    .WithEnvironment("Facilities:CarbonCopyCalendarId", facilitiesCarbonCopyCalendarId);

var app = builder
    .AddViteApp("temasek-webapp", "../Temasek.WebApp")
    .WithPnpm()
    .PublishAsDockerFile()
    .PublishAsDockerComposeService((_, _) => { })
    .WithReference(api)
    .WithEnvironment("NUXT_PUBLIC_CLERK_PUBLISHABLE_KEY", clerkPublishableKey);

builder.Build().Run();
