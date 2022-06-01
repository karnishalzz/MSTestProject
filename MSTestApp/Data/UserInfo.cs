using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MSTestApp
{
    public class UserInfo
    {
        [Key]
        public Guid Id { get; set; }
        public string Key { get; set; }

        public string Email { get; set; }

        public string Attribute { get; set; }
        public bool IsMailSent { get; set; }

        public DateTime CreatedDate { get; set; }
    }

}
