using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentGateCheck
{
public class APICheckObject
    {
        public string ApiName { get; set; }
        public int ApiStatus { get; set; }
    }

    public class DeploymentResult
    {
        public int totalSuccess { get; set; }
        public APICheckObject[] details { get; set; }
    }
}
