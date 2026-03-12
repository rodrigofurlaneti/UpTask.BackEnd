using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpTask.Domain.Exceptions;

namespace UpTask.Domain.ValueObjects
{
    public sealed record HexColor
    {
        public static readonly HexColor Default = new("#607D8B");
        public static readonly HexColor ProjectDefault = new("#1976D2");

        public string Value { get; }

        public HexColor(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("Color cannot be empty.");

            if (!IsValidHex(value))
                throw new DomainException($"'{value}' is not a valid hex color. Expected format: #RRGGBB or #RGB.");

            Value = value.ToUpperInvariant();
        }

        private static bool IsValidHex(string color) =>
            color.StartsWith('#') &&
            (color.Length == 7 || color.Length == 4) &&
            color[1..].All(c => "0123456789ABCDEFabcdef".Contains(c));

        public override string ToString() => Value;
    }
}
