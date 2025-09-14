using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManager.Context;
using TaskManager.DTO;
using TaskManager.Model;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        public readonly IMongoCollection<User> mongoCollection;
        public readonly IConfiguration configuration;
        public UserController(MongoDBContext dBContext,IConfiguration configurationa)
        {
            mongoCollection = dBContext.User;
            configuration = configurationa;
        }
        [NonAction]
        public string Generator(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Name),
                    new Claim(ClaimTypes.Name, user.Name),
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);

        }

        [HttpPost("LogIn")]
        public async Task<IActionResult> LogIn([FromBody] LoginDto loginDto)
        {
            var username = loginDto.username;
            var password = loginDto.password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return Ok("KHông ĐƯợc để trống tên đăng nhập hoặc mật khẩu!");
            }
            var filter = Builders<User>.Filter.Eq(a => a.Name, username);
            var data = await mongoCollection.Find(filter).ToListAsync();
            if (data == null || data.Count == 0)
            {
                return Ok("Tên Người Dùng Không TỒn Tại");
            }
            if (data[0].Password != password)
            {
                return Ok("Mật Khẩu Không Đúng");
            }
            if (data.Where(a => a.Name.ToLower() == username.ToLower() && a.Password == password).Any())
            {
                var token = Generator(data[0]);
                return Ok(new { token = token, user = data[0] });
            }
            return Ok("Đăng Nhập Thất Bại");
        }
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if(string.IsNullOrEmpty(registerDto.Username)||string.IsNullOrEmpty(registerDto.Email) || string.IsNullOrEmpty(registerDto.Password))
            {
                return Ok("Tên Đăng Nhập Hoặc Mật Khẩu Không Được Để Trống");
            }
             var filter= Builders<User>.Filter.Eq(a => a.Name, registerDto.Username);
            var data = await mongoCollection.Find(filter).ToListAsync();
            if (data!=null&&data.Count>0)
            {
                return Ok("Tên Người Dùng Đã Tồn Tại");
            }
            var datas=new User
            {
                id=Guid.NewGuid().ToString(),
                Name = registerDto.Username,
                Email = registerDto.Email,
                Password= registerDto.Password,
                Task=new List<TaskProperty>()
            };
            await mongoCollection.InsertOneAsync(datas);
            return Ok(new
            {
                token=Generator(datas),
                User= datas
            });
        }
    }

}
