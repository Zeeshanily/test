 public async Task<List<UserCouponResponse>> GetUserCoupn(UserCouponRequest request)
 {
   apiClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_appSettings.OneOpsCredentialForFinanceV2.UserName}:{_appSettings.OneOpsCredentialForFinanceV2.Password}")));
   HttpResponseMessage httpResponse = await apiClient.GetAsync(_appSettings.Urls.FinanceV2 + UrlsConfig.FinanceV2Operations.GetUserCoupons(request.Email,request.EmployeeId));
   httpResponse.EnsureSuccessStatusCode();
   var response =  JsonSerializer.Deserialize<List<UserCouponResponse>>(await httpResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true});
   return response;
 }
