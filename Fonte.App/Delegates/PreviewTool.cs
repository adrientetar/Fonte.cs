/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;

    using Windows.UI;

    public class PreviewTool : BaseTool
    {
        public override object FindResource(DesignCanvas canvas, object resourceKey)
        {
            var key = (string)resourceKey;
            if (key == DesignCanvas.DrawAnchorsKey ||
                key == DesignCanvas.DrawGuidelinesKey ||
                key == DesignCanvas.DrawMetricsKey ||
                key == DesignCanvas.DrawPointsKey ||
                key == DesignCanvas.DrawSelectionKey ||
                key == DesignCanvas.DrawStrokeKey)
            {
                return false;
            }
            else if (key == DesignCanvas.ComponentColorKey ||
                     key == DesignCanvas.FillColorKey)
            {
                return Colors.Black;
            }

            return base.FindResource(canvas, resourceKey);
        }
    }
}
