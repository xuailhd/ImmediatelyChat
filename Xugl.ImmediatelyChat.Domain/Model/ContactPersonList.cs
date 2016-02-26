using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xugl.ImmediatelyChat.Model
{
    public class ContactPersonList
    {
        private string ContactPerson { get; set; }

        public string objectID { get; set; }

        public string DestinationObjectID { get; set; }

        public bool IsDelete { get; set; }

        public DateTime UpdateTime { get; set; }
    }
}
