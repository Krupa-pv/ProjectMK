using System;
using Microsoft.Maui.Graphics;
using System.Diagnostics;


namespace MK.Drawables;

public class BoundingBoxDrawable:IDrawable
{
    public List<BoundingBoxResult> BoundingBoxes { get; set; }
    public float ImageWidth { get; set; }
    public float ImageHeight { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Colors.Red;
        canvas.StrokeSize = 6;
        canvas.DrawLine(10, 10, 90, 100);

        /*foreach (var box in BoundingBoxes)
        {
            // Adjust bounding box coordinates for the image
            float left = box.Left * ImageWidth;
            Debug.WriteLine(left);

            float top = box.Top * ImageHeight;
            Debug.WriteLine(top);

            float width = box.Width * ImageWidth;
            Debug.WriteLine(width);

            float height = box.Height * ImageHeight;
            Debug.WriteLine(height);


            // Draw the bounding box
            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(left, top, width, height);

            // Optionally, add the label
            canvas.FillColor = Colors.Red;

            Debug.WriteLine(box.Label);
        }
        */
    }

}

public class BoundingBoxResult
{
    public string Label { get; set; }
    public float Left { get; set; }
    public float Top { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}