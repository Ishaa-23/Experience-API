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
        public ActionResult<User> Register(UserDto request,string role)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.Username = request.UserName;
            user.PasswordHash = passwordHash;
            user.Role = role;
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



        [HttpGet, Authorize]
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
                        return results.Where(x => x.isPermission == true).ToList();  //only allowing operation on permission=yes
                    }
                    else
                    {
                        return BadRequest("Empty List");
                    }

                }

            }
        }
        [HttpPost, Authorize(Roles = "Supervisor,Manager")]
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
        [HttpPut("Update"), Authorize(Roles ="Supervisor,Manager")]
        public async Task<ActionResult> Update(Employee1 newEmp)
        {

            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                HttpResponseMessage res = await client.GetAsync("api/Employee");
                string responseContent = res.Content.ReadAsStringAsync().Result;
                List<Employee1> results = JsonConvert.DeserializeObject<List<Employee1>>(responseContent);
                var emp = results.FirstOrDefault(x => x.Id == newEmp.Id);
                if(emp==null)
                {
                    return NotFound("No employee of this id exists.");
                }
                else
                {
                    if (emp.isPermission == false)
                    {
                        return BadRequest("No operation is allowed.");
                    }
                    else
                    {
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
                            var rc = response.Content.ReadAsStringAsync().Result;
                            response.EnsureSuccessStatusCode();
                            List<Employee1> list = JsonConvert.DeserializeObject<List<Employee1>>(rc);
                            Log.Information("Put results => {@result}", list);
                            return Ok("Updated successfully at: " + DateTime.Now);
                        }
                    }
                }
                

            }

        }
        [HttpPut("Soft-delete"), Authorize(Roles = "Supervisor,Manager")]
        public async Task<ActionResult> SoftDelete(int id)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(url);
                HttpResponseMessage res = await httpClient.GetAsync("api/Employee");
                string responseContent = res.Content.ReadAsStringAsync().Result;
                List<Employee1> results = JsonConvert.DeserializeObject<List<Employee1>>(responseContent);
                var newEmp = results.FirstOrDefault(x => x.Id == id);
                if(newEmp==null)
                {
                    return NotFound("No employee of this id exists.");
                }
                else
                {
                    if (newEmp.isPermission == false)
                    {
                        return BadRequest("No operation allowed.");
                    }
                    else
                    {
                        var postData = new
                        {
                            Id = newEmp.Id,
                            Name = newEmp.Name,
                            Age = newEmp.Age,
                            Role = newEmp.Role,
                            isDeleted = newEmp.isDeleted,
                            isActive = false,
                            isPermission = newEmp.isPermission

                        };
                        var content = new StringContent(JsonConvert.SerializeObject(postData), Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await httpClient.PutAsync($"api/Employee/update", content);
                        if (response.IsSuccessStatusCode)
                        {
                            return Ok("Soft-deleted at: " + DateTime.Now);
                        }
                        else
                        {
                            return BadRequest("Soft-delete failed");
                        }
                    }
                }
                

            }
        }
        [HttpPut("Revoke-permission"), Authorize(Roles ="Supervisor")]
        public async Task<ActionResult<List<Employee1>>> Permission(int id)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                HttpResponseMessage res = await client.GetAsync("api/Employee");
                string responseContent = res.Content.ReadAsStringAsync().Result;
                List<Employee1> results = JsonConvert.DeserializeObject<List<Employee1>>(responseContent);
                var newEmp = results.FirstOrDefault(x => x.Id == id);
                if(newEmp==null)
                {
                    return NotFound("No employee of this id exists.");
                }
                else
                {
                    if (newEmp.isPermission == false)
                    {
                        return Ok("Permission already revoked.");
                    }
                    else
                    {
                        var postData = new
                        {
                            Id = newEmp.Id,
                            Name = newEmp.Name,
                            Age = newEmp.Age,
                            Role = newEmp.Role,
                            isDeleted = newEmp.isDeleted,
                            isActive = newEmp.isActive,
                            isPermission = false

                        };
                        var content = new StringContent(JsonConvert.SerializeObject(postData), Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await client.PutAsync($"api/Employee/update", content);
                        if (response.IsSuccessStatusCode)
                        {
                            return Ok("Permission Revoked at: " + DateTime.Now);
                        }
                        else
                        {
                            return BadRequest("Permission Revoke failed");
                        }
                    }
                }
                

            }
        }
        [HttpPut("Grant-permission"), Authorize(Roles = "Supervisor")]

        public async Task<ActionResult> GrantPermission(int id)
        {
            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                HttpResponseMessage res = await client.GetAsync("api/Employee");
                string responseContent = res.Content.ReadAsStringAsync().Result;
                List<Employee1> results = JsonConvert.DeserializeObject<List<Employee1>>(responseContent);
                var newEmp = results.FirstOrDefault(x => x.Id == id);
                if(newEmp==null)
                {
                    return NotFound("No employee of this id exists.");
                }
                else
                {
                    if (newEmp.isPermission == true)
                    {
                        return Ok("Permission already granted.");
                    }
                    else
                    {
                        var postData = new
                        {
                            Id = newEmp.Id,
                            Name = newEmp.Name,
                            Age = newEmp.Age,
                            Role = newEmp.Role,
                            isDeleted = newEmp.isDeleted,
                            isActive = newEmp.isActive,
                            isPermission = true

                        };
                        var content = new StringContent(JsonConvert.SerializeObject(postData), Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await client.PutAsync($"api/Employee/update", content);
                        if (response.IsSuccessStatusCode)
                        {
                            return Ok("Permission Granted at :" + DateTime.Now);
                        }
                        else
                        {
                            return BadRequest("Permission Grant failed");
                        }
                    }
                }
                

            }
        }


        [HttpDelete("Delete"), Authorize(Roles = "Supervisor")]
        public async Task<ActionResult> Delete(int id)
        {

            string url = configuration.GetSection("AppSettings").GetSection("url").Value;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                HttpResponseMessage res = await client.GetAsync("api/Employee");
                string responseContent = res.Content.ReadAsStringAsync().Result;
                List<Employee1> results = JsonConvert.DeserializeObject<List<Employee1>>(responseContent);
                var newEmp = results.FirstOrDefault(x => x.Id == id);
                if(newEmp==null)
                {
                    return NotFound("No employee of this id exists.");
                }
                else
                {
                    if (newEmp.isPermission == false)
                    {
                        return BadRequest("No operation allowed.");
                    }
                    else
                    {
                        using (HttpResponseMessage response = await client.DeleteAsync("api/Employee/delete?id=" + id))
                        {
                            var rc = response.Content.ReadAsStringAsync().Result;

                            //response.EnsureSuccessStatusCode();
                            if (response.IsSuccessStatusCode)
                            {
                                Log.Information("Deleted results of Id => {@id}", id);
                                return Ok("Deleted at: " + DateTime.Now);

                            }
                            else
                            {
                                return BadRequest("Delete failed.");
                            }

                        }
                    }
                }
               
            }
        }
        [HttpPost]
        private string CreateToken(User user)
        {
           
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                 configuration.GetSection("AppSettings:Token").Value!));

            var roles = new[] { "Supervisor", "Manager", "Employee" };
            var roleClaims = new[] { new Claim(ClaimTypes.Role, user.Role) };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(roleClaims),
                Expires = DateTime.UtcNow.AddHours(1), // Token expiration time
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // Serialize the token to a string
            var tokenString = tokenHandler.WriteToken(token);
            return tokenString;

        }
    }
}