using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CriticalCommonLib.Services.Mediator;

namespace InventorySorting.Mediator
{
    public record ToggleGenericWindowMessage(Type windowType) : MessageBase;
    public record ItemSearchRequestedMessage() : MessageBase;
    public record OpenGenericWindowMessage(Type windowType) : MessageBase;
}
