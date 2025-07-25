﻿
using Microsoft.Maui.Graphics;

namespace BPMNEngine.Drawing.Icons.IconParts
{
    internal class InnerCircle(bool dashed) : IIconPart
    {
        private const float _PEN_SIZE = 2f;

        public InnerCircle()
            : this(false) { }

        public void Add(ICanvas surface, int iconSize, Color color)
        {
            surface.StrokeColor = color;
            surface.StrokeSize = _PEN_SIZE;
            surface.StrokeDashPattern = dashed ? Constants.DASH_PATTERN : null;
            surface.DrawEllipse(new Rect(4, 4, AIcon.IMAGE_SIZE - 8, AIcon.IMAGE_SIZE- 8));
        }
    }
}
