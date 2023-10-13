using ExperienceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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
        [HttpPost("register")]
        public ActionResult<User> Register(UserDto request)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.Username = request.UserName;
            user.PasswordHash = passwordHash;
            return Ok(user);
        }
        [HttpPost("login")]
        public ActionResult<User> Login(UserDto request)
        {
            if (user.Username != request.UserName)
            {
                return BadRequest("User not found");
            }
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Wrong password");
            }

            string token = CreateToken(user);
            return Ok(token);
        }



        [HttpGet,Authorize]
        public async Task<ActionResult<List<Employee1>>> GetAll()
        {
             string url = configuration.GetSection("AppSettings").GetSection("url").Value;
                using (var client = new HttpClient())
                {

                    client.BaseAddress = new Uri(url);
                    using (HttpResponseMessage response = await client.GetAsync("api/Employee"))
                    {
                        string responseContent = response.Content.ReadAsStringAsync().Result;
                        response.EnsureSuccessStatusCode();
                        List<Employee1> results = JsonConvert.DeserializeObject<List<Employee1>>(responseContent);
                        if (results.ToList().Count > 0)
                        {
                            Log.Information("Get results => {@result}", results);
                            return results;
                        }
                        else
                        {
                            return BadRequest("Empty List");
                        }

                    }

                }
            

        }
        [HttpPost,Authorize]
        public async Task<ActionResult> Add(Employee1 emp)
        {

                string url = configuration.GetSection("AppSettings").GetSection("url").Value;
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(url);
                
                    var postData = new
                    {
                        Name = emp.Name,
                        Age = emp.Age,
                        Role= emp.Role,
                        isDeleted= emp.isDeleted,
                        isActive= emp.isActive,
                        isPermission= emp.isPermission


                    };
                    var content = new StringContent(JsonConvert.SerializeObject(postData), Encoding.UTF8, "application/json");


                    using (HttpResponseMessage response = await client.PostAsync("api/Employee/Add", content))
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        response.EnsureSuccessStatusCode();
                        List<Employee1> results = JsonConvert.DeserializeObject<List<Employee1>>(responseContent);
                        Log.Information("Post results => {@result}", results);
                        return Ok("Added");

                    }
                }
            
        }
        [HttpPut,Authorize]
        public async Task<ActionResult<List<Employee1>>> Update(Employee1 newEmp)
        {
            
                string url = configuration.GetSection("AppSettings").GetSection("url").Value;
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(url);
                    var postData = new
                    {
                        Id = newEmp.Id,
                        Name = newEmp.Name,
                        Age = newEmp.Age,
                        Role = newEmp.Role,
                        isDeleted = newEmp.isDeleted,
                        isActive = newEmp.isActive,
                        isPermission = newEmp.isPermission

                    };
                    var content = new StringContent(JsonConvert.SerializeObject(postData), Encoding.UTF8, "application/json");
                    using (HttpResponseMessage response = await client.PutAsync("api/Employee/update", content))
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        response.EnsureSuccessStatusCode();
                        List<Employee1> results = JsonConvert.DeserializeObject<List<Employee1>>(responseContent);
                        Log.Information("Put results => {@result}", results);
                        return results;
                    }
                }
           
        }
        [HttpDelete("soft-delete"), Authorize]
        public async Task<ActionResult<List<Employee1>>> SoftDelete(int id)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                using (HttpResponseMessage response = await client.DeleteAsync("api/Employee/soft-delete?id=" + id))

                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Soft-Deleted results of Id => {@id}", id);
                        return Ok("Deleted");

                    }
                    else
                    {
                        return BadRequest("Delete failed.");
                    }
                }
            }
        }
        [HttpDelete("permission"), Authorize]
        public async Task<ActionResult<List<Employee1>>> Permission(int id)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                using (HttpResponseMessage response = await client.DeleteAsync("api/Employee/permission?id=" + id))

                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Changed permission of {@id}", id);
                        return Ok("Permission changed");

                    }
                    else
                    {
                        return BadRequest("Failed operation.");
                    }
                }
            }
        }


        [HttpDelete,Authorize]
        public async Task<ActionResult> Delete( int id)
        {
           
                string url = configuration.GetSection("AppSettings").GetSection("url").Value;
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(url);
                    using (HttpResponseMessage response = await client.DeleteAsync("api/Employee/delete?id=" + id))
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;

                        //response.EnsureSuccessStatusCode();
                        if (response.IsSuccessStatusCode)
                        {
                            Log.Information("Deleted results of Id => {@id}", id);
                            return Ok("Deleted");

                        }
                        else
                        {
                            return BadRequest("Delete failed.");
                        }

                    }
                }
            

        }
        [HttpPost]
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim> {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role,"Admin")
                                

            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                configuration.GetSection("AppSettings:Token").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var token = new JwtSecurityToken(
                    claims:claims,
                    expires: DateTime.Now.AddMinutes(10),
                    signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}