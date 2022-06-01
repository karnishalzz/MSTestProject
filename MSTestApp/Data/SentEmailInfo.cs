using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MSTestApp
{
    public class SentEmailInfo
    {
        [Key]
        public Guid Id { get; set; }

        public string Email { get; set; }

        public string Message { get; set; }

        public DateTime SentTime { get; set; }
    }

}
