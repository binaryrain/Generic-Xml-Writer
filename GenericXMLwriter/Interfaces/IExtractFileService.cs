using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericXmlCreator.Interfaces
{
    public interface IExtractFileService
    {
        List<string> WriteRecord(DataTable MainDataDt, string Type);
    }
}
