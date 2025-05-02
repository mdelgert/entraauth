The error message you're encountering in your backend:

```
CORS
Network Failure
URL scheme must be "http" or "https" for CORS request.
```

indicates that the frontend is attempting to make a CORS (Cross-Origin Resource Sharing) request to the backend, but the URL scheme used in the request is invalid (i.e., it’s not `http` or `https`). This issue is likely caused by one of the following:

1. **Incorrect URL in Frontend**: The frontend is using an invalid or malformed URL to call the backend (e.g., missing the scheme, using `localhost` instead of the service name, or using a non-standard scheme).
2. **HTTPS Redirection Conflict**: The backend is configured to enforce HTTPS redirection (`app.UseHttpsRedirection()`), but the frontend is attempting to use HTTP, or there’s a mismatch in the expected protocol.
3. **CORS Misconfiguration**: Although your backend has a CORS policy allowing all origins, methods, and headers, there might be an issue with how the CORS middleware is interacting with the request.

Based on your provided `docker-compose.yml` and backend code, I’ll explain why this error is occurring, how to fix it, and ensure your frontend can successfully call the backend. I’ll also address the URL for REST calls and any relevant configurations in your backend.

---

### **Analysis of Your Setup**

#### **docker-compose.yml Recap**
From your `docker-compose.yml`:
- **Frontend**:
  - Listens on HTTP port `8080` and HTTPS port `8081` inside the container.
  - Host port `5000` maps to container port `8081` (HTTPS).
- **Backend**:
  - Listens on HTTP port `8080` and HTTPS port `8081` inside the container.
  - Host port `5001` maps to container port `8081` (HTTPS).
- **Correct URL for Frontend to Call Backend**: As established, the frontend should use:
  ```
  http://backend:8080
  ```
  for HTTP requests to the backend, using the service name `backend` and the internal HTTP port `8080`.

#### **Backend Code Analysis**
Your backend code includes:
- **CORS Policy**:
  ```csharp
  builder.Services.AddCors(o => o.AddPolicy("default", builder =>
  {
      builder.AllowAnyOrigin()
             .AllowAnyMethod()
             .AllowAnyHeader();
  }));
  app.UseCors("default");
  ```
  This allows all origins, methods, and headers, which is permissive and should allow CORS requests from the frontend (e.g., from `https://localhost:5000` or `http://frontend:8080`).

- **HTTPS Redirection**:
  ```csharp
  app.UseHttpsRedirection();
  ```
  This middleware redirects HTTP requests to HTTPS. If the frontend tries to call `http://backend:8080`, the backend may respond with a redirect to `https://backend:8081`, which could cause issues if the frontend’s `HttpClient` or browser doesn’t handle the redirect properly or if the HTTPS certificate isn’t trusted.

- **Authentication**:
  ```csharp
  builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddMicrosoftIdentityWebApi(...);
  app.UseAuthentication();
  app.UseAuthorization();
  ```
  Your backend uses Microsoft Entra ID (Azure AD) for JWT-based authentication. If the frontend’s request lacks a valid JWT token or the token is invalid, the backend may reject the request, potentially contributing to the error.

- **Swagger**: Enabled in development/test environments, redirecting the root (`/`) to `/swagger`.

---

### **Why the Error Occurs**
The error message suggests the frontend is making a CORS request with an invalid URL scheme. Possible causes include:
1. **Frontend Using Incorrect URL**:
   - The frontend might be calling the backend with a URL like `backend:8080/api/values` (missing the `http://` scheme) or using a non-standard scheme.
   - Example: If the frontend code uses `fetch("backend:8080/api/values")` in JavaScript or `new Uri("backend:8080")` in C#, the scheme is missing, causing the CORS error.
2. **HTTPS Redirection**:
   - The backend’s `app.UseHttpsRedirection()` is redirecting `http://backend:8080` to `https://backend:8081`. If the frontend’s `HttpClient` doesn’t follow redirects or the HTTPS certificate isn’t trusted, the request fails.
3. **CORS and Browser Interaction**:
   - If the frontend is a SPA (e.g., Blazor, React, Angular) running in the browser, the browser enforces CORS. The error might occur if the browser’s CORS preflight request (`OPTIONS`) fails due to a scheme mismatch or redirection.
4. **Authentication Issue**:
   - If the backend endpoint requires authentication and the frontend doesn’t send a valid JWT token, the backend might return an error that’s misinterpreted as a CORS issue.

---

### **Solution**

To resolve the error and ensure the frontend can call the backend, follow these steps:

#### **1. Verify and Fix Frontend URL**
Ensure the frontend uses the correct URL with the `http` scheme:
```
http://backend:8080
```
- **For C# (ASP.NET Core Frontend)**:
  Update the `HttpClient` configuration in `Program.cs`:
  ```csharp
  builder.Services.AddHttpClient("Backend", client =>
  {
      client.BaseAddress = new Uri("http://backend:8080/");
  });
  ```
  Example usage in a controller:
  ```csharp
  private readonly IHttpClientFactory _clientFactory;
  public MyController(IHttpClientFactory clientFactory)
  {
      _clientFactory = clientFactory;
  }
  public async Task<IActionResult> CallApi()
  {
      var client = _clientFactory.CreateClient("Backend");
      var response = await client.GetStringAsync("api/values");
      ViewData["Message"] = response;
      return View();
  }
  ```

- **For JavaScript (SPA Frontend, e.g., React, Angular)**:
  Update the API call to include the scheme:
  ```javascript
  fetch("http://backend:8080/api/values")
      .then(response => response.json())
      .then(data => console.log(data))
      .catch(error => console.error("Error:", error));
  ```
  If using a library like Axios:
  ```javascript
  axios.get("http://backend:8080/api/values")
      .then(response => console.log(response.data))
      .catch(error => console.error("Error:", error));
  ```

- **Check for Missing Scheme**:
  If the frontend code omits the scheme (e.g., `new Uri("backend:8080")` or `fetch("backend:8080/api/values")`), add `http://` explicitly. For example:
  ```csharp
  // Incorrect
  client.BaseAddress = new Uri("backend:8080/");
  // Correct
  client.BaseAddress = new Uri("http://backend:8080/");
  ```

#### **2. Disable HTTPS Redirection for Container-to-Container Communication**
The backend’s `app.UseHttpsRedirection()` is likely causing the frontend’s HTTP request (`http://backend:8080`) to be redirected to `https://backend:8081`, which may fail due to certificate issues or the frontend not following redirects. Since container-to-container communication doesn’t require HTTPS in development, disable HTTPS redirection for internal requests.

Modify the backend’s `Program.cs` to bypass HTTPS redirection for container-to-container calls:
```csharp
var builder = WebApplication.CreateBuilder(args);

// ... (other services)

builder.Services.AddCors(o => o.AddPolicy("default", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
}));

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
{
    app.UseSwagger();
    app.UseSwaggerUI(ModernStyle.Dark);
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/")
        {
            context.Response.Redirect("/swagger");
            return;
        }
        await next();
    });
}

app.UseCors("default");

// Conditionally apply HTTPS redirection only for external requests
app.Use(async (context, next) =>
{
    // Skip HTTPS redirection for requests from the frontend container
    if (context.Request.Host.Host == "backend" && context.Request.Host.Port == 8080)
    {
        await next();
    }
    else
    {
        await next();
        if (context.Response.StatusCode == 200 && context.Request.Scheme == "http")
        {
            var httpsPort = context.Request.Host.Port == 8080 ? 8081 : context.Request.Host.Port;
            var httpsUrl = $"https://{context.Request.Host.Host}:{httpsPort}{context.Request.Path}{context.Request.QueryString}";
            context.Response.Redirect(httpsUrl);
        }
    }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

Alternatively, disable HTTPS redirection entirely for development:
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

This ensures `http://backend:8080` requests from the frontend are not redirected to HTTPS, preventing scheme-related errors.

#### **3. Verify CORS Configuration**
Your CORS policy is permissive (`AllowAnyOrigin`, `AllowAnyMethod`, `AllowAnyHeader`), which should work for development. However, ensure the CORS middleware is applied before routing and controllers:
```csharp
app.UseCors("default");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```
This order is correct in your code, so CORS should not be the issue unless the frontend’s request is malformed.

If the frontend is a browser-based SPA, ensure the browser’s CORS preflight (`OPTIONS`) requests are handled correctly. Your current CORS policy allows preflight requests, but test by making a simple GET request to confirm:
- Use the browser’s DevTools (Network tab) to check the request and response headers.
- Ensure the backend responds with:
  ```
  Access-Control-Allow-Origin: *
  Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
  Access-Control-Allow-Headers: *
  ```

#### **4. Handle Authentication**
Your backend uses Microsoft Entra ID authentication, which requires a valid JWT token. If the frontend’s request lacks a token or sends an invalid token, the backend may reject the request, potentially contributing to the error.

- **Check Frontend Authentication**:
  Ensure the frontend includes a valid JWT token in the `Authorization` header:
  ```csharp
  var client = _clientFactory.CreateClient("Backend");
  client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "<your-jwt-token>");
  var response = await client.GetStringAsync("api/values");
  ```
  For a SPA, include the token in JavaScript:
  ```javascript
  fetch("http://backend:8080/api/values", {
      headers: {
          "Authorization": "Bearer <your-jwt-token>"
      }
  })
      .then(response => response.json())
      .then(data => console.log(data))
      .catch(error => console.error("Error:", error));
  ```

- **Acquire Token**:
  Use the Microsoft Authentication Library (MSAL) to acquire a token for Entra ID. For example, in a C# frontend:
  ```csharp
  builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd");
  builder.Services.AddControllersWithViews(options =>
  {
      var policy = new AuthorizationPolicyBuilder()
          .RequireAuthenticatedUser()
          .Build();
      options.Filters.Add(new AuthorizeFilter(policy));
  });
  ```
  Then use MSAL to get a token and include it in requests.

- **Bypass Authentication for Testing**:
  To isolate the CORS issue, temporarily disable authentication for a test endpoint in the backend:
  ```csharp
  [AllowAnonymous]
  [HttpGet("api/test")]
  public IActionResult Test()
  {
      return Ok("Test endpoint");
  }
  ```
  Call `http://backend:8080/api/test` from the frontend. If this works, the issue is related to authentication rather than CORS or the URL scheme.

#### **5. Test and Debug**
- **Test the Backend Directly**:
  - Open a browser and navigate to `https://localhost:5001/swagger` to verify the backend is running and accessible.
  - Try a test endpoint (e.g., `https://localhost:5001/api/test` if you added the `[AllowAnonymous]` endpoint).
  - If HTTPS fails, test HTTP on port `8080` inside the container:
    ```bash
    docker exec -it <backend_container_id> curl http://localhost:8080/api/test
    ```

- **Check Frontend Request**:
  - If the frontend is a SPA, use the browser’s DevTools (Network tab) to inspect the failing request. Look for:
    - The exact URL being called (e.g., `backend:8080` vs. `http://backend:8080`).
    - Response status (e.g., `307 Temporary Redirect` for HTTPS redirection).
    - Any CORS-related headers.
  - If the frontend is an ASP.NET Core app, enable logging for `HttpClient`:
    ```csharp
    builder.Services.AddHttpClient("Backend", client =>
    {
        client.BaseAddress = new Uri("http://backend:8080/");
    }).AddLogger<HttpClient>();
    ```

- **Check Container Logs**:
  - In Visual Studio, use the **Containers** window (View > Other Windows > Containers) to view backend logs.
  - Look for errors like “Failed to bind to address” or authentication failures.
  - Alternatively, run:
    ```bash
    docker logs <backend_container_id>
    ```

- **Verify Network**:
  - Confirm both containers are on the same network:
    ```bash
    docker network ls
    docker network inspect <network_name>
    ```
    Both `frontend` and `backend` should be listed under the same network.

#### **6. Update docker-compose.yml (Optional)**
Your `docker-compose.yml` maps both services to HTTPS ports (`8081`). To support HTTP communication and avoid redirection issues, expose the HTTP port (`8080`) for the backend:
```yaml
services:
  frontend:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
      - ASPNETCORE_HTTPS_PORTS=8081
    ports:
      - "5000:8081"
    depends_on:
      - backend
  backend:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
      - ASPNETCORE_HTTPS_PORTS=8081
    ports:
      - "5001:8081"
      - "5002:8080" # Add HTTP port for external testing
```

This allows you to test the backend’s HTTP endpoint externally at `http://localhost:5002`. However, the frontend should still use `http://backend:8080` internally.

---

### **Recommended Fixes Summary**
1. **Fix Frontend URL**:
   - Use `http://backend:8080` explicitly in the frontend code.
   - Example: `client.BaseAddress = new Uri("http://backend:8080/");` in C# or `fetch("http://backend:8080/api/values")` in JavaScript.
2. **Disable HTTPS Redirection**:
   - Modify the backend to skip HTTPS redirection for `http://backend:8080` requests (see updated `Program.cs` above).
   - Alternatively, disable `app.UseHttpsRedirection()` in development.
3. **Handle Authentication**:
   - Ensure the frontend sends a valid JWT token for Entra ID-protected endpoints.
   - Test with an `[AllowAnonymous]` endpoint to isolate CORS issues.
4. **Test and Debug**:
   - Verify the backend at `https://localhost:5001/swagger` or `http://localhost:5002/api/test` (if you add the HTTP port).
   - Check frontend requests in DevTools or logs.
   - Inspect container logs for errors.

---

### **Example: Test Endpoint**
Add this to your backend’s `Controllers` folder (e.g., `TestController.cs`):
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/["

System: controller]")]
public class TestController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Backend is reachable!");
    }
}
```

From the frontend, call:
```
http://backend:8080/api/test
```
- In C#:
  ```csharp
  var client = _clientFactory.CreateClient("Backend");
  var response = await client.GetStringAsync("api/test");
  Console.WriteLine(response); // Should print: Backend is reachable!
  ```
- In JavaScript:
  ```javascript
  fetch("http://backend:8080/api/test")
      .then(response => response.text())
      .then(data => console.log(data)) // Should print: Backend is reachable!
      .catch(error => console.error("Error:", error));
  ```

If this works, the CORS and URL scheme issues are resolved, and you can focus on authentication for protected endpoints.

---

### **Troubleshooting Tips**
- **If the Error Persists**:
  - Share the exact frontend code making the API call (e.g., `HttpClient` setup or JavaScript `fetch`).
  - Provide the full error message from the browser’s DevTools (Network tab) or backend logs.
  - Check if the frontend is running in a browser (SPA) or server-side (ASP.NET Core).
- **Authentication Issues**:
  - If the `[AllowAnonymous]` endpoint works but protected endpoints fail, verify the JWT token acquisition process in the frontend.
  - Ensure the `AzureAd` configuration in `appsettings.json` matches between frontend and backend.
- **Certificate Issues**:
  - If you must use HTTPS (`https://backend:8081`), uncomment the certificate volumes in `docker-compose.yml` and regenerate development certificates:
    ```bash
    dotnet dev-certs https --clean
    dotnet dev-certs https
    ```

---

### **Final Notes**
- The primary fix is ensuring the frontend uses `http://backend:8080` and disabling HTTPS redirection for internal communication.
- Your CORS policy is correctly configured, so the issue is likely the URL scheme or HTTPS redirection.
- Authentication may be a secondary issue if protected endpoints are involved.

If you provide the frontend code or more details about the error (e.g., browser console output, exact URL being called), I can pinpoint the issue further. Let me know!