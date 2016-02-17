using Xugl.ImmediatelyChat.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xugl.ImmediatelyChat.Data.EF.Mapping.Site
{
    public class MMSServerMapping : EntityTypeConfiguration<MMSServer>
    {
        public MMSServerMapping()
        {
            this.HasKey(t => t.MMS_IP);
            this.HasKey(t => t.MMS_Port);
        }
    }

    //public class WarehouseMapping: EntityTypeConfiguration<Warehouse>
    //{
    //    public WarehouseMapping()
    //    {
    //        this.HasKey(t => t.WarehouseID);
    //    }
    //}

    //public class ContactGroupMapping : EntityTypeConfiguration<ContactGroup>
    //{
    //    public ContactGroupMapping()
    //    {
    //        this.HasKey(t => t.GroupObjectID);
    //    }
    //}

    //public class ContactGroupSubMapping : EntityTypeConfiguration<ContactGroupSub>
    //{
    //    public ContactGroupSubMapping()
    //    {
    //        //this.HasKey(t => t.ContactGroupID);
    //        this.HasKey(t => t.ContactGroupID);
    //        this.HasKey(t => t.ContactPersonObjectID);
    //    }
    //}

    //public class ContactPersonMapping : EntityTypeConfiguration<ContactPerson>
    //{
    //    public ContactPersonMapping()
    //    {
    //        this.HasKey(t => t.ObjectID);
    //    }
    //}

    //public class ContactPersonListMapping : EntityTypeConfiguration<ContactPersonList>
    //{
    //    public ContactPersonListMapping()
    //    {
    //        this.HasKey(t => t.objectID);
    //        this.HasKey(t => t.DestinationObjectID);
    //    }
    //}

    //public class MsgRecordMapping : EntityTypeConfiguration<MsgRecord>
    //{
    //    public MsgRecordMapping()
    //    {
    //        this.HasKey(t => t.MsgID);
    //    }
    //}
}
