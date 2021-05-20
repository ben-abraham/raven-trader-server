using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace raven_trader_server.Models
{
    public class RC_Asset
    {
        [Key]
        public int ID { get; set; }

        public string Name { get; set; }
    }
}
