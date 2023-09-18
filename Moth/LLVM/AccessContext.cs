using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moth.LLVM;

public class AccessContext
{
    public ValueContext ValueContext { get; set; }
    public Class Class { get; set; }

    public AccessContext(ValueContext valueContext, Class @class)
    {
        ValueContext = valueContext;
        Class = @class;
    }
}
