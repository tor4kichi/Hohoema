using System;

namespace Hohoema.Models;

[Flags]
public enum CommentCommandPermissionType
{
    Owner = 0x01,
    User = 0x02,
    Anonymous = 0x04,
}
