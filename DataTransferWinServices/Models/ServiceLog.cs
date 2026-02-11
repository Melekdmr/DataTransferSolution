using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace DataTransferWinService.Models
{
    public class ServiceLog
    {
        public string MethodName { get; set; }
        public string Description { get; set; }
        public Exception Ex { get; set; }
    }
}
