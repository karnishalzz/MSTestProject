using Microsoft.EntityFrameworkCore;
using MSTestApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity.Validation;
using Azure.Storage.Blobs;
using System.Text;
using Azure.Storage.Blobs.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using MSTestApp.Utilities;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;

namespace MSTestApp.Services
{
    public class TestService : ITestService
    {
        private readonly AppDbContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private IHostingEnvironment _env;
        public TestService(AppDbContext dbContext, BlobServiceClient blobServiceClient,IHostingEnvironment env)
        {
            _context = dbContext;
            _blobServiceClient = blobServiceClient;
            _env = env;

        }

        public async Task<object> ProcessData(List<TestModel> datas)
        {
           
            UserInfo item;
            int TotalAttributeCount = 0;
         

            try
            {

                foreach (var data in datas)
                {
                    //Create a file in Blob storage for the unique mails in every day
                    await StoreEmailInBlobAsync(data.Email);

                    //Save the json information in database and return the new attribute count
                    Tuple<UserInfo, int> tuple = await SaveUserInfoInDatabase(data);
                    item = tuple.Item1;
                    TotalAttributeCount = tuple.Item2;

                    //check if the unique attribute count is equal to 10 and if any previous mail is sent to the user
                    if (TotalAttributeCount == 10 && !item.IsMailSent)
                    {
                        //send mail to user who got 10 new attributes
                        var sentEmailId = await SendEmailToUserWithTenAttributes(item);

                        //update the table information in database
                        await UpdateTableInfo(item.Id,sentEmailId);
                    }
                }


                return new
                {
                    Success = true,
                    Message = "Successfully Processed",

                };
            }
            catch (DbEntityValidationException e)
            {
                return new
                {
                    Success = false,
                    Message = string.Join(" || ", e.EntityValidationErrors.SelectMany(x => x.ValidationErrors.Select(ve => ve.ErrorMessage).ToList()).ToList())
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Success = false,
                    ex.Message
                };
            }
        }

        private async Task StoreEmailInBlobAsync(string email)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient("email");
                var blobClient = containerClient.GetBlobClient(email);

                if (!await blobClient.ExistsAsync())
                {
                    using (var ms = new MemoryStream())
                    {
                        await blobClient.UploadAsync(ms, true);
                    }

                }
            }
            catch
            {
                throw new Exception("Failed to upload log in Blob storage");
            }

        }

        private async Task<Tuple<UserInfo,int>> SaveUserInfoInDatabase(TestModel model)
        {
            UserInfo item = null;
            bool isEdit = true;
            int TotalAttributeCount = 0;
            try
            {
                item = await _context.UserInfos.Where(x => x.Email == model.Email && x.CreatedDate == DateTime.Today).FirstOrDefaultAsync();
                if (item == null)
                {
                    item = new UserInfo()
                    {
                        Email = model.Email,
                        Key = model.Key,
                        CreatedDate = DateTime.Today,
                        Attribute = string.Join(",", model.Attributes),
                        IsMailSent = false
                    };

                    isEdit = false;
                    TotalAttributeCount = model.Attributes.Count();
                }
                else
                {
                    var existingAttributes = item.Attribute.Trim().Split(",").ToList();
                    var uniqueAttributes = model.Attributes.Except(existingAttributes).ToList();
                    if (uniqueAttributes.Count > 0)
                        item.Attribute = item.Attribute + "," + string.Join(",", uniqueAttributes);

                    TotalAttributeCount = existingAttributes.Count + uniqueAttributes.Count;
                }

                if (!isEdit)
                {
                    _context.UserInfos.Add(item);
                }
                else
                {
                    _context.Entry(item).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();
                return Tuple.Create(item, TotalAttributeCount);
            }
            catch
            {
                throw new Exception("Incorrect configuration settings.");
            }

        }

        private async Task<Guid> SendEmailToUserWithTenAttributes(UserInfo item)
        {
            SentEmailInfo emailInfo = null;
            try
            {
                emailInfo = new SentEmailInfo()
                {
                    Email = item.Email,
                    Message = "Congratulate!<br /><br />We have received following 10 unique attributes from you: " + item.Attribute + ".<br /><br />Best regards, Millisecond",
                    SentTime = DateTime.Now,
                };
                _context.SentEmailInfos.Add(emailInfo);

                await _context.SaveChangesAsync();

                var em = new EmailSender();
                await em.SendAsync("Millisecond", item.Email, null, null, "10 New Attributes!", emailInfo.Message);

                #region BlobForEmailMsg
              
                var containerClient = _blobServiceClient.GetBlobContainerClient("sent-emails");
               
               
                var path = _env.ContentRootPath + "\\SentMails";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string postedFileExtension = ".txt";
                string newName = Guid.NewGuid().ToString() + emailInfo.Email + postedFileExtension;
                string newPath = Path.Combine(path, newName);
                   
                using (FileStream fs = System.IO.File.Create(newPath))
                {
                    byte[] content = new UTF8Encoding(true).GetBytes(emailInfo.Message);

                    fs.Write(content, 0, content.Length);
                      
                }
                var blobClient = containerClient.GetBlobClient(newName);

                if (!await blobClient.ExistsAsync())
                {
                    
                    await blobClient.UploadAsync(newPath, true);
                }
                if (File.Exists(newPath))
                {
                    // If file found, delete it    
                    File.Delete(newPath);
                }
                #endregion
                return emailInfo.Id;
            }
            catch(Exception ex)
            {
                throw new Exception("Operation Failed");
            }

        } 
      
        private async Task UpdateTableInfo(Guid userId,Guid SentEmailId)
        {
            try
            {
                var userInfo = await _context.UserInfos.Where(x => x.Id == userId).FirstOrDefaultAsync();
                userInfo.IsMailSent = true;
                _context.Entry(userInfo).State = EntityState.Modified;

                var sentEmailInfo = await _context.SentEmailInfos.Where(x => x.Id == SentEmailId).FirstOrDefaultAsync();
                sentEmailInfo.SentTime = DateTime.Now;
                _context.Entry(sentEmailInfo).State = EntityState.Modified;

                await _context.SaveChangesAsync();
            }
            catch
            {
                throw new Exception("Could not update info after sending mail");
            }

        }

    }


    public interface ITestService
    {
        Task<object> ProcessData(List<TestModel> data);
    }
}