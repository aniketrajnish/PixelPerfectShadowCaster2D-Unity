using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Makra.Rendering.Tools
{
    /// <summary>
    /// static class to calculate pixel perfect paths for sprites
    /// </summary>
    public static class PixelPerfectSpritePathCalculator
    {
        /// <summary>
        /// traces the sprite to generate pixel perfect polygons
        /// </summary>
        public static Vector2Int[][] TraceSprite(Sprite sprite, float alphaThreshold)
        {
            if (sprite == null)
                return new Vector2Int[0][];

            Texture2D texture = sprite.texture;
            RectInt rect = new RectInt((int)sprite.rect.x, (int)sprite.rect.y, (int)sprite.rect.width, (int)sprite.rect.height);
            Color[] pixelData = texture.GetPixels(rect.x, rect.y, rect.width, rect.height);
            bool[] solidityMap = new bool[pixelData.Length];

            for (int i = 0; i < pixelData.Length; i++)
                solidityMap[i] = pixelData[i].a > alphaThreshold; // pixel solid if alpha > threshold

            int width = rect.width;
            int height = rect.height;

            bool currentLineSegmentNull = true;
            LineSegmentDirection currentLineSegmentDirection = LineSegmentDirection.RIGHT;
            LineSegment currentLineSegment = new LineSegment();
            LinkedList<LineSegment> rightLineSegments = new LinkedList<LineSegment>();
            LinkedList<LineSegment> leftLineSegments = new LinkedList<LineSegment>();
            LinkedList<LineSegment> upLineSegments = new LinkedList<LineSegment>();
            LinkedList<LineSegment> downLineSegments = new LinkedList<LineSegment>();

            // ---- horizontal tracing pass ----
            for (int y = 0; y <= height; y++) // rows -> y columns -> x
            {
                for (int x = 0; x < width; x++)
                {
                    // check if pixel above (y) and below (y-1) are solid
                    bool upSideSolid = IsPixelSolidOnMap(x, y, solidityMap, width, height);
                    bool downSideSolid = IsPixelSolidOnMap(x, y - 1, solidityMap, width, height);
                    
                    // case 1: transition from transparenet (above) to solid (below) -- right facing horizontal edge
                    if (!upSideSolid && downSideSolid)
                    {
                        if (currentLineSegmentNull || currentLineSegmentDirection != LineSegmentDirection.RIGHT)
                        {
                            CompleteLineSegment(
                                ref currentLineSegmentNull,
                                currentLineSegmentDirection,
                                currentLineSegment,
                                rightLineSegments,
                                leftLineSegments,
                                upLineSegments,
                                downLineSegments
                            );

                            currentLineSegment.Start = new Vector2Int(x, y);
                            currentLineSegment.End = new Vector2Int(x + 1, y);
                            currentLineSegmentDirection = LineSegmentDirection.RIGHT;
                            currentLineSegmentNull = false;
                        }
                        else
                            currentLineSegment.End = new Vector2Int(currentLineSegment.End.x + 1, currentLineSegment.End.y);
                        
                    }
                    // case 2: transition from solid (above) to transparent (below) -- left facing horizontal edge
                    else if (upSideSolid && !downSideSolid)
                    {
                        if (currentLineSegmentNull || currentLineSegmentDirection != LineSegmentDirection.LEFT)
                        {
                            CompleteLineSegment(
                                ref currentLineSegmentNull,
                                currentLineSegmentDirection,
                                currentLineSegment,
                                rightLineSegments,
                                leftLineSegments,
                                upLineSegments,
                                downLineSegments
                            );

                            currentLineSegment.Start = new Vector2Int(x + 1, y);
                            currentLineSegment.End = new Vector2Int(x, y);
                            currentLineSegmentDirection = LineSegmentDirection.LEFT;
                            currentLineSegmentNull = false;
                        }
                        else
                            currentLineSegment.Start = new Vector2Int(currentLineSegment.Start.x + 1, currentLineSegment.Start.y);

                    }
                    // case 3: no transition -- complete any pending line segment
                    else
                    {
                        CompleteLineSegment(
                            ref currentLineSegmentNull,
                            currentLineSegmentDirection,
                            currentLineSegment,
                            rightLineSegments,
                            leftLineSegments,
                            upLineSegments,
                            downLineSegments
                        );
                    }
                }
            }
            // complete any pending line segment after horizontal traicing
            CompleteLineSegment(
                ref currentLineSegmentNull,
                currentLineSegmentDirection,
                currentLineSegment,
                rightLineSegments,
                leftLineSegments,
                upLineSegments,
                downLineSegments
            );

            // ---- horizontal tracing pass ----
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // check if pixel to the right (x) and left (x-1) are solid
                    bool rightSideSolid = IsPixelSolidOnMap(x, y, solidityMap, width, height);
                    bool leftSideSolid = IsPixelSolidOnMap(x - 1, y, solidityMap, width, height);

                    // case 1: transition from transparenet (left) to solid (right) -- up facing vertical edge
                    if (rightSideSolid && !leftSideSolid)
                    {
                        if (currentLineSegmentNull || currentLineSegmentDirection != LineSegmentDirection.UP)
                        {
                            CompleteLineSegment(
                                ref currentLineSegmentNull,
                                currentLineSegmentDirection,
                                currentLineSegment,
                                rightLineSegments,
                                leftLineSegments,
                                upLineSegments,
                                downLineSegments
                            );

                            currentLineSegment.Start = new Vector2Int(x, y);
                            currentLineSegment.End = new Vector2Int(x, y + 1);
                            currentLineSegmentDirection = LineSegmentDirection.UP;
                            currentLineSegmentNull = false;
                        }
                        else
                            currentLineSegment.End = new Vector2Int(currentLineSegment.End.x, currentLineSegment.End.y + 1);
                        
                    }
                    // case 2: transition from solid (left) to transparent (right) -- down facing vertical edge
                    else if (!rightSideSolid && leftSideSolid)
                    {
                        if (currentLineSegmentNull || currentLineSegmentDirection != LineSegmentDirection.DOWN)
                        {
                            CompleteLineSegment(
                                ref currentLineSegmentNull,
                                currentLineSegmentDirection,
                                currentLineSegment,
                                rightLineSegments,
                                leftLineSegments,
                                upLineSegments,
                                downLineSegments
                            );

                            currentLineSegment.Start = new Vector2Int(x, y + 1);
                            currentLineSegment.End = new Vector2Int(x, y);
                            currentLineSegmentDirection = LineSegmentDirection.DOWN;
                            currentLineSegmentNull = false;
                        }
                        else
                            currentLineSegment.Start = new Vector2Int(currentLineSegment.Start.x, currentLineSegment.Start.y + 1);
                        
                    }
                    // case 3: no transition -- complete any pending line segment
                    else
                    {
                        CompleteLineSegment(
                            ref currentLineSegmentNull,
                            currentLineSegmentDirection,
                            currentLineSegment, rightLineSegments,
                            leftLineSegments,
                            upLineSegments,
                            downLineSegments
                        );
                    }
                }
            }
            // complete any pending line segment after vertical traicing
            CompleteLineSegment(
                ref currentLineSegmentNull,
                currentLineSegmentDirection,
                currentLineSegment,
                rightLineSegments,
                leftLineSegments,
                upLineSegments,
                downLineSegments
            );


            LinkedList<Vector2Int[]> polygons = new LinkedList<Vector2Int[]>();

            // ---- polygon construction pass ----
            // iterate over all line segments and construct polygons
            while (leftLineSegments.Count + rightLineSegments.Count + upLineSegments.Count + downLineSegments.Count > 0)
            {
                LinkedList<Vector2Int> currentPolygon = new LinkedList<Vector2Int>();
                currentPolygon.AddFirst(rightLineSegments.First.Value.Start);
                currentPolygon.AddLast(rightLineSegments.First.Value.End);
                rightLineSegments.RemoveFirst();
                LineSegmentDirection lastLineSegmentDirection = LineSegmentDirection.RIGHT;

                while (currentPolygon.First.Value != currentPolygon.Last.Value)
                    AddLineSegment(
                        currentPolygon, 
                        ref lastLineSegmentDirection, 
                        rightLineSegments, 
                        leftLineSegments, 
                        upLineSegments, 
                        downLineSegments
                    );
                
                currentPolygon.RemoveLast();
                polygons.AddLast(currentPolygon.ToArray());
            }
            return polygons.ToArray();
        }


        private static bool IsPixelSolidOnMap(int x, int y, bool[] solidityMap, int width, int height) =>
            // check if a pixel is solid on the solidity map
            !(x < 0 || y < 0 || x >= width || y >= height) && solidityMap[(y * width) + x];


        private static void CompleteLineSegment(
            ref bool currentLineSegmentNull,
            LineSegmentDirection currentLineSegmentDirection,
            LineSegment currentLineSegment,
            LinkedList<LineSegment> rightLineSegments,
            LinkedList<LineSegment> leftLineSegments,
            LinkedList<LineSegment> upLineSegments,
            LinkedList<LineSegment> downLineSegments
        )
        {
            if (currentLineSegmentNull)
            {
                return;
            }
            LinkedList<LineSegment> segmentsList = currentLineSegmentDirection switch
            {
                LineSegmentDirection.RIGHT => rightLineSegments,
                LineSegmentDirection.LEFT => leftLineSegments,
                LineSegmentDirection.UP => upLineSegments,
                LineSegmentDirection.DOWN => downLineSegments,
                _ => null,
            };
            segmentsList?.AddLast(currentLineSegment);
            currentLineSegmentNull = true;
        }
        private static void AddLineSegment(
            LinkedList<Vector2Int> partialPolygon,
            ref LineSegmentDirection lastLineSegmentDirection,
            LinkedList<LineSegment> rightLineSegments,
            LinkedList<LineSegment> leftLineSegments,
            LinkedList<LineSegment> upLineSegments,
            LinkedList<LineSegment> downLineSegments
        )
        {
            // add line segment to the partial polygon being constructed
            if (lastLineSegmentDirection == LineSegmentDirection.RIGHT)
            {
                if (_AddLineSegment(partialPolygon, downLineSegments)) { lastLineSegmentDirection = LineSegmentDirection.DOWN; return; }
                if (_AddLineSegment(partialPolygon, upLineSegments)) { lastLineSegmentDirection = LineSegmentDirection.UP; return; }
            }
            else if (lastLineSegmentDirection == LineSegmentDirection.LEFT)
            {
                if (_AddLineSegment(partialPolygon, upLineSegments)) { lastLineSegmentDirection = LineSegmentDirection.UP; return; }
                if (_AddLineSegment(partialPolygon, downLineSegments)) { lastLineSegmentDirection = LineSegmentDirection.DOWN; return; }
            }
            else if (lastLineSegmentDirection == LineSegmentDirection.UP)
            {
                if (_AddLineSegment(partialPolygon, rightLineSegments)) { lastLineSegmentDirection = LineSegmentDirection.RIGHT; return; }
                if (_AddLineSegment(partialPolygon, leftLineSegments)) { lastLineSegmentDirection = LineSegmentDirection.LEFT; return; }
            }
            else if (lastLineSegmentDirection == LineSegmentDirection.DOWN)
            {
                if (_AddLineSegment(partialPolygon, leftLineSegments)) { lastLineSegmentDirection = LineSegmentDirection.LEFT; return; }
                if (_AddLineSegment(partialPolygon, rightLineSegments)) { lastLineSegmentDirection = LineSegmentDirection.RIGHT; return; }
            }
        }

        /// <summary>
        /// adds a line segment to the partial polygon if it connects
        /// </summary>
        private static bool _AddLineSegment(LinkedList<Vector2Int> partialPolygon, LinkedList<LineSegment> lineSegments)
        {
            Vector2Int lastPointInPolygon = partialPolygon.Last.Value;
            for (LinkedListNode<LineSegment> node = lineSegments.First; node != null; node = node.Next)
            {
                if (node.Value.Start == lastPointInPolygon)
                {
                    partialPolygon.AddLast(node.Value.End);
                    lineSegments.Remove(node);
                    return true;
                }
            }
            return false;
        }
    }

    // --- Helper Structs and Enums ---
    public enum LineSegmentDirection : byte
    {
        RIGHT,
        LEFT,
        UP,
        DOWN,
    }

    public struct LineSegment
    {
        public Vector2Int Start;
        public Vector2Int End;
        public LineSegment(Vector2Int start, Vector2Int end)
        {
            Start = start;
            End = end;
        }
    }
}