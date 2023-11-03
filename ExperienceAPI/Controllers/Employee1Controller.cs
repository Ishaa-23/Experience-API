using ExperienceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;
using System;
using System.Data;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ExperienceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Employee1Controller : ControllerBase
    {
        private IConfiguration configuration;
        public Employee1Controller(IConfiguration iConfig)
        {
            configuration = iConfig;
        }

        public static User user = new User();
        public string TokenToUse;
        
        [HttpGet("Login")]
        public async Task<ActionResult> GetToken(string Username, string Password)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            string requestUri = $"{url}/api/Employee/login";
            string username = Username;
            string password = Password;

            using (var client = new HttpClient())
            {

                UriBuilder uriBuilder = new UriBuilder(requestUri);
                uriBuilder.Query = $"Username={username}&Password={password}";
                HttpResponseMessage response = await client.GetAsync(uriBuilder.Uri);

                if (response.IsSuccessStatusCode)
                {
                    
                    string token = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

                        if (jwtToken == null)
                        {
                            // Token cannot be read
                            return Ok("Token cannot be read.");
                        }

                        var now = DateTime.UtcNow;
                        if (jwtToken.ValidTo < now)
                        {
                            // Token has expired
                            return BadRequest("Token has expired.");
                        }

                        // Token is still valid
                        TokenToUse = token;
                        Response.Cookies.Append("JWTToken", TokenToUse, new CookieOptions
                        {
                            Expires = DateTimeOffset.Now.AddHours(1),
                            Path = "/",
                        }); ;
                        return Ok(TokenToUse);
                    }
                    catch (HttpRequestException e)
                    {
                        return BadRequest(e.Message);
                    }
                }
                else
                {
                    return BadRequest();
                }
            }
        }
     
        [HttpGet("Display-Info")]
        public async Task<ActionResult<List<Employee1>>> DisplayInfo(UserRoles role, string name)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    string userRole;
                    if ((int)role == 1) { userRole = "Supervisor"; }
                    else if ((int)role == 2) { userRole = "Manager"; }
                    else { userRole = "Employee"; }

                    var t = Request.Cookies["JWTToken"];
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t);
                    string requestUri = $"{url}/api/Employee/Display-Info?role={userRole}&name={name}";
                    HttpResponseMessage response = await httpClient.GetAsync(requestUri);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        if (userRole == "Employee")
                        {
                            Employee1 emp = JsonConvert.DeserializeObject<Employee1>(responseContent);
                            return Ok(emp);
                        }
                        List<Employee1> results = JsonConvert.DeserializeObject<List<Employee1>>(responseContent);
                        return Ok(results);
                    }
                    else
                    {
                        return BadRequest("Error");
                    }
                }
                catch (HttpRequestException e)
                {
                    return Ok(e.Message);
                }
            }
        }



        [HttpPost("Add-Employees")]
        public async Task<ActionResult> Add(Employee1 emp)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    var t = Request.Cookies["JWTToken"];
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t);
                    string requestUri = $"{url}/api/Employee/Add-Employees";
                    DateTime dateTime = DateTime.Now; // Replace with your DateTime value
                    string formattedDate = dateTime.ToString("MMMM dd, yyyy HH:mm:ss tt");
                    var newEmp = new {
                        Name = emp.Name,
                        Age = emp.Age,
                        Salary = emp.Salary,
                        Department = emp.Department,
                        LastModified = formattedDate,
                        isActive = true,
                        isPermission = true,
                        isDeleted = false
                    };
                    string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(newEmp);
                    var response = await httpClient.PostAsync(requestUri, new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        return Ok(responseContent);
                    }
                    else
                    {
                        return BadRequest("Error");
                    }


                }
                catch (HttpRequestException e)
                {
                    return Ok(e.Message);
                }
            }
        }
        [HttpPost("Add-EmpRoles")]
        public async Task<ActionResult> AddEmpRoles(string name, UserRoles roles)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    var t = Request.Cookies["JWTToken"];
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t);
                    string requestUri = $"{url}/api/EmpRole/Add-EmpRole?name={name}&role={roles}";
                    var response = await httpClient.PostAsync(requestUri,null);
                    if(response.IsSuccessStatusCode)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        return Ok(responseContent);
                    }
                    else
                    {
                        return BadRequest();
                    }
                }
                catch (HttpRequestException e) 
                {
                    return BadRequest(e.Message);
                }
                }
        }
        [HttpDelete("Delete-EmpRoles")]
        public async Task<ActionResult> DeleteEmpRoles(string name, UserRoles roles)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    httpClient.BaseAddress = new Uri(url);
                    var t = Request.Cookies["JWTToken"];
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t);
                    HttpResponseMessage response = await httpClient.DeleteAsync($"api/EmpRole/Delete-EmpRole?name={name}&role={roles}");
                    if(response.IsSuccessStatusCode)
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        return Ok(responseContent);
                    }
                    else
                    {
                        return BadRequest("Could not delete.");
                    }
                }
                catch(HttpRequestException e)
                {
                    return BadRequest(e.Message);
                }
            }
        }




        [HttpDelete("Delete-Employee")]
        public async Task<ActionResult> Delete(int id)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    httpClient.BaseAddress = new Uri(url);
                    var t = Request.Cookies["JWTToken"];
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t);
                    HttpResponseMessage response = await httpClient.DeleteAsync($"api/Employee/Delete?id={id}");
                    if(response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        return Ok(result);
                    }
                    else
                    {
                        return BadRequest();
                    }
                }
                catch (HttpRequestException e)
                {
                    return Ok(e.Message);
                }
            }
        }
        [HttpPut("Update-Employee")]
        public async Task<ActionResult> Update(Employee1 emp)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (var httpClient = new HttpClient())
            {
               
                httpClient.BaseAddress = new Uri(url);
                DateTime dateTime = DateTime.Now; 
                string formattedDate = dateTime.ToString("MMMM dd, yyyy HH:mm:ss tt");
                
                var updatedEmployee = new Employee1
                {
                    Emp_Id = emp.Emp_Id, 
                    Age = emp.Age, 
                    Name = emp.Name,
                    Salary=emp.Salary,
                    Department= emp.Department,
                    LastModified= formattedDate,
                    
                   
                };

                
                var content = new StringContent(JsonConvert.SerializeObject(updatedEmployee), System.Text.Encoding.UTF8, "application/json");

                var t = Request.Cookies["JWTToken"];
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t);
                HttpResponseMessage response = await httpClient.PutAsync("api/Employee/Update", content);

                if (response.IsSuccessStatusCode)
                {
                   
                    string result = await response.Content.ReadAsStringAsync();
                    return Ok(result);
                }
                else
                {
                    
                    string error = await response.Content.ReadAsStringAsync();
                    return BadRequest(error);   
                }
            }
        }
           
        }
    }
