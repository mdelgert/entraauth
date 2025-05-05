# https://learn.microsoft.com/en-us/entra/identity-platform/optional-claims?tabs=appui#configuring-group-optional-claims

To restrict access to a specific page in your Blazor application to users who are members of the "TestGroup" group in Microsoft Entra ID (formerly Azure AD), you need to configure your Azure App Registration and modify your Blazor application code to enforce group-based authorization. Below is a step-by-step guide to achieve this, assuming you are using a Blazor Server or Blazor WebAssembly app with Microsoft Entra ID authentication.

---

### **Steps in Azure App Registration (Microsoft Entra ID)**

1. **Ensure Your App Registration is Configured for Authentication**
   - Navigate to the **Azure Portal** > **Microsoft Entra ID** > **App Registrations**.
   - Select your existing app registration or create a new one if needed.
   - Verify that the app is configured for your authentication scenario:
     - For **Blazor Server**, set the platform to **Web** and add a redirect URI (e.g., `https://localhost:<port>/signin-oidc` for development).
     - For **Blazor WebAssembly**, set the platform to **Single-page application (SPA)** and add a redirect URI (e.g., `https://localhost:<port>/authentication/login-callback`).
     - Ensure **Supported account types** is set appropriately (e.g., "Accounts in this organizational directory only" for single-tenant apps).
   - Note the **Application (client) ID** and **Directory (tenant) ID** for use in your Blazor app configuration.

2. **Enable Group Claims in the App Registration**
   - In the Azure Portal, go to your app registration.
   - Navigate to **Token configuration** > **Add groups claim**.
   - Select the **Security groups** option to include group membership claims in the ID or access token.
     - Choose the **Group ID** format (emits the Object ID of the group) for simplicity.
     - Ensure the **ID token** and/or **Access token** options are checked, depending on your authentication flow (ID token is typically used for Blazor apps).
   - Save the changes.
   - **Note**: If you have a Microsoft Entra ID Premium license, you can assign groups directly to the app. If not, you can still use group claims with a standard license, as described here.

3. **Assign Users to the TestGroup**
   - Go to **Microsoft Entra ID** > **Groups** in the Azure Portal.
   - Locate or create the "TestGroup" security group.
     - If it doesn’t exist, create a new security group and note its **Object ID** (you’ll need this in your code).
   - Add the desired users to the "TestGroup" as members.
   - **Optional (Premium license required)**: Restrict app access to specific users or groups:
     - Go to **Microsoft Entra ID** > **Enterprise applications** > Select your app.
     - Under **Properties**, set **User assignment required?** to **Yes**.
     - Go to **Users and groups**, and assign the "TestGroup" to the app to allow only its members to sign in.

4. **Grant Permissions for Group Membership Retrieval (Optional for Graph API)**
   - If you plan to use the Microsoft Graph API to retrieve group memberships programmatically (e.g., for more complex scenarios), add the following permission:
     - Go to **API permissions** > **Add a permission** > **Microsoft Graph** > **Application permissions**.
     - Add `GroupMember.Read.All` for the app to read group memberships.
     - Grant admin consent for the permission.
   - This step is optional if you’re only using group claims in the token, as described above.

---

### **Steps in Your Blazor Application**

#### **1. Configure Authentication in Your Blazor App**
Ensure your Blazor app is set up to authenticate with Microsoft Entra ID. Update your app’s configuration to include the necessary settings.

- **For Blazor Server**:
  - In `appsettings.json`, add the Entra ID configuration:
    ```json
    {
      "AzureAd": {
        "Instance": "https://login.microsoftonline.com/",
        "Domain": "<your-tenant-domain>", // e.g., contoso.onmicrosoft.com
        "TenantId": "<your-tenant-id>", // Directory (tenant) ID from Azure
        "ClientId": "<your-client-id>", // Application (client) ID from Azure
        "CallbackPath": "/signin-oidc"
      }
    }
    ```
  - In `Program.cs`, configure authentication:
    ```csharp
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.Identity.Web;

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

    builder.Services.AddAuthorization(options =>
    {
        // Optional: Add a policy for TestGroup if using policy-based authorization
        options.AddPolicy("TestGroupOnly", policy =>
            policy.RequireClaim("groups", "<TestGroup-Object-ID>"));
    });

    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();

    var app = builder.Build();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    app.Run();
    ```

- **For Blazor WebAssembly**:
  - In `wwwroot/appsettings.json`, add:
    ```json
    {
      "AzureAd": {
        "Authority": "https://login.microsoftonline.com/<your-tenant-id>",
        "ClientId": "<your-client-id>",
        "ValidateAuthority": true
      }
    }
    ```
  - In `Program.cs`, configure MSAL for authentication:
    ```csharp
    using Microsoft.AspNetCore.Components.Authorization;
    using Microsoft.Authentication.WebAssembly.Msal;

    var builder = WebAssemblyHostBuilder.CreateDefault(args);

    builder.Services.AddMsalAuthentication(options =>
    {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions);
        options.ProviderOptions.DefaultAccessTokenScopes.Add("User.Read");
        options.ProviderOptions.AdditionalTokenRequestParameters.Add("groupMembershipClaims", "SecurityGroup");
    });

    builder.Services.AddAuthorizationCore(options =>
    {
        // Optional: Add a policy for TestGroup
        options.AddPolicy("TestGroupOnly", policy =>
            policy.RequireClaim("groups", "<TestGroup-Object-ID>"));
    });

    await builder.Build().RunAsync();
    ```
  - Replace `<TestGroup-Object-ID>` with the Object ID of the "TestGroup" from the Azure Portal.

#### **2. Restrict Access to a Specific Page**

You can restrict access to a Blazor page using either the `<AuthorizeView>` component, the `[Authorize]` attribute, or a custom authorization policy. Here’s how to do it for a page that should only be accessible to users in the "TestGroup".

- **Option 1: Using `<AuthorizeView>` in Razor Components**
  - In your Razor page (e.g., `RestrictedPage.razor`), use the `<AuthorizeView>` component to check for the group claim:
    ```razor
    @page "/restricted-page"
    @using Microsoft.AspNetCore.Authorization
    @using System.Security.Claims

    <h3>Restricted Page</h3>

    <AuthorizeView Policy="TestGroupOnly">
        <Authorized>
            <p>Welcome! You are a member of TestGroup.</p>
        </Authorized>
        <NotAuthorized>
            <p>Sorry, you are not authorized to view this page.</p>
        </NotAuthorized>
    </AuthorizeView>
    ```
  - This assumes you’ve defined the `TestGroupOnly` policy in `Program.cs` as shown above, checking for the group’s Object ID in the `groups` claim.

- **Option 2: Using the `[Authorize]` Attribute**
  - Apply the `[Authorize]` attribute to the page and specify the policy:
    ```razor
    @page "/restricted-page"
    @using Microsoft.AspNetCore.Authorization
    @attribute [Authorize(Policy = "TestGroupOnly")]

    <h3>Restricted Page</h3>
    <p>Welcome! You are a member of TestGroup.</p>
    ```
  - If the user is not in the "TestGroup," they will be redirected to the app’s unauthorized page or login page (depending on configuration).

- **Option 3: Manual Group Check in Code**
  - If you prefer not to use policies, you can manually check the user’s group membership in the page’s code:
    ```razor
    @page "/restricted-page"
    @using Microsoft.AspNetCore.Components.Authorization
    @inject AuthenticationStateProvider AuthenticationStateProvider

    <h3>Restricted Page</h3>

    @if (isInTestGroup)
    {
        <p>Welcome! You are a member of TestGroup.</p>
    }
    else
    {
        <p>Sorry, you are not authorized to view this page.</p>
    }

    @code {
        private bool isInTestGroup = false;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            isInTestGroup = user.HasClaim(c => c.Type == "groups" && c.Value == "<TestGroup-Object-ID>");
        }
    }
    ```
  - Replace `<TestGroup-Object-ID>` with the actual Object ID of the "TestGroup."

#### **3. Handle Unauthorized Access**
- By default, Blazor redirects unauthorized users to the login page or displays a "Not Authorized" message.
- To customize the unauthorized experience, create a page (e.g., `NotAuthorized.razor`) and configure the app to redirect there:
  - In `App.razor`, update the `AuthorizeRouteView`:
    ```razor
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="typeof(MainLayout)">
                <NotAuthorized>
                    <p>Sorry, you are not authorized to access this page.</p>
                    <a href="/">Go to Home</a>
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
        <NotFound>
            <p>Page not found.</p>
        </NotFound>
    </Router>
    ```

---

### **Testing and Validation**

1. **Test Locally**
   - Run your Blazor app locally and sign in with a user who is a member of "TestGroup" to verify they can access the restricted page.
   - Sign in with a user who is not in "TestGroup" to confirm they are denied access or redirected.

2. **Use Incognito Mode**
   - Test in an incognito/private browser session to avoid cached credentials, as recommended by Microsoft.[](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/microsoft-entra-id-groups-and-roles?view=aspnetcore-8.0)

3. **Verify Group Claims**
   - To debug, display the user’s claims in a Razor page to ensure the `groups` claim includes the TestGroup’s Object ID:
     ```razor
     @page "/claims"
     @using System.Security.Claims
     @inject AuthenticationStateProvider AuthenticationStateProvider

     <h3>Your Claims</h3>
     <ul>
         @foreach (var claim in claims)
         {
             <li>@claim.Type: @claim.Value</li>
         }
     </ul>

     @code {
         private IEnumerable<Claim> claims = new List<Claim>();

         protected override async Task OnInitializedAsync()
         {
             var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
             claims = authState.User.Claims;
         }
     }
     ```

4. **Deploy and Test in Production**
   - Deploy your app to Azure App Service or another hosting platform.
   - Update the redirect URI in the app registration to match the production URL (e.g., `https://<app-name>.azurewebsites.net/signin-oidc`).
   - Test the restricted page with users in and out of "TestGroup."

---

### **Additional Notes**

- **Group Claims Limitation**: If a user is a member of many groups, Microsoft Entra ID may not include all group claims in the token due to token size limits. In such cases, you may need to use the Microsoft Graph API to query group memberships programmatically. This requires additional configuration (e.g., `GroupMember.Read.All` permission) and code to call the Graph API.[](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/microsoft-entra-id-groups-and-roles-net-5-to-7?view=aspnetcore-7.0)
- **Premium License Requirement**: Assigning groups directly to an app (via "User assignment required") requires a Microsoft Entra ID Premium license. However, using group claims in tokens, as described above, works with a standard license.[](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/microsoft-entra-id-groups-and-roles?view=aspnetcore-8.0)
- **Security Best Practices**:
  - Use the Secret Manager tool for local development to store sensitive credentials securely.[](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-9.0)
  - Avoid storing credentials in code or configuration files for production environments; use managed identities if deploying to Azure.[](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-9.0)
- **References**:
  - Microsoft Learn: Restrict a Microsoft Entra app to a set of users.[](https://learn.microsoft.com/en-us/entra/identity-platform/howto-restrict-your-app-to-a-set-of-users)
  - Microsoft Learn: ASP.NET Core Blazor WebAssembly with Microsoft Entra ID groups and roles.[](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/microsoft-entra-id-groups-and-roles?view=aspnetcore-8.0)
  - Microsoft Learn: Manage users and groups assignment to an application.[](https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/assign-user-or-group-access-portal)

---

By following these steps, you’ll successfully restrict access to a specific page in your Blazor application to users who are members of the "TestGroup" in Microsoft Entra ID. If you encounter issues, verify the group Object ID, check the token claims, and ensure the app registration settings are correct. Let me know if you need further clarification or assistance!