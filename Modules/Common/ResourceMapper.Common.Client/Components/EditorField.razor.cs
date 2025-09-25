using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMapper.Common.Client.Components
{
    public partial class EditorField
    {
        [Parameter] public string? Label { get; set; }
        [Parameter] public RenderFragment? Input { get; set; }
    }
}
