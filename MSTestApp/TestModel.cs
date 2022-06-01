using System;
using System.Collections.Generic;

namespace MSTestApp
{
    public class TestModel
    {
        public string Key { get; set; }

        public string Email { get; set; }

        public ICollection<string> Attributes { get; set; }
    }

}
