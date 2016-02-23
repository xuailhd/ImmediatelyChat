using Xugl.ImmediatelyChat.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xugl.ImmediatelyChat.Data.EF.Mapping.Site
{
    public class MMSServerMapping : EntityTypeConfiguration<MMSServer>
    {
        public MMSServerMapping()
        {
            this.HasKey(t => t.MMS_IP);
            this.HasKey(t => t.MMS_Port);

            this.Property(t => t.MMS_Port).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
        }
    }


    public class ContactPersonMapping : EntityTypeConfiguration<ContactPerson>
    {
        public ContactPersonMapping()
        {
            this.HasKey(t => t.ObjectID);
        }
    }
}
