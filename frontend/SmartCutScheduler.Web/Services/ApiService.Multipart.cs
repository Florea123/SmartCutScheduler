using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace SmartCutScheduler.Web.Services;

public partial class ApiService
{
    public async Task<HttpResponseMessage> PutMultipartAsync(string endpoint, MultipartFormDataContent content)
    {
        await AddAuthHeaderAsync();
        return await _http.PutAsync(endpoint, content);
    }
}