using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FuroAutomaticoRevit.Core
{
    public static class GeometryUtils
    {
        public static Solid GetIntersectionSolid(Solid pipeSolid, Solid slabSolid)
        {
            try
            {
                return BooleanOperationsUtils.ExecuteBooleanOperation(
                    pipeSolid,
                    slabSolid,
                    BooleanOperationsType.Intersect
                );
            }
            catch
            {
                return null;
            }
        }

        public static XYZ GetCentroid(Solid solid)
        {
            BoundingBoxXYZ bbox = solid.GetBoundingBox();
            return (bbox.Min + bbox.Max) / 2;
        }

        public static Solid GetElementSolid(Element element, Transform transform = null)
        {
            Options options = new Options { ComputeReferences = true };
            GeometryElement geometry = element.get_Geometry(options);

            if (geometry == null) return null;

            Solid solid = null;
            foreach (GeometryObject obj in geometry)
            {
                if (obj is Solid s && s.Volume > 0.0001)
                {
                    solid = s;
                    break;
                }

                if (obj is GeometryInstance instance)
                {
                    foreach (GeometryObject instObj in instance.GetInstanceGeometry())
                    {
                        if (instObj is Solid instSolid && instSolid.Volume > 0.0001)
                        {
                            solid = instSolid;
                            break;
                        }
                    }
                }

                if (solid != null) break;
            }

            if (solid == null) return null;

            // Apply coordinate transformation if needed
            if (transform != null)
            {
                solid = SolidUtils.CreateTransformed(solid, transform);
            }

            return solid;
        }

        public static Curve GetPipeCenterline(Element pipe)
        {
            if (pipe.Location is LocationCurve curveLoc)
            {
                return curveLoc.Curve;
            }
            return null;
        }

        public static bool IsElementInView(Element e, View view)
        {
            try
            {
                return !e.IsHidden(view);
            }
            catch
            {
                return true; // Fallback
            }
        }

        public static bool OutlineContains(Outline outline, XYZ point, double tolerance = 0.001)
        {
            return point.X >= outline.MinimumPoint.X - tolerance &&
                   point.Y >= outline.MinimumPoint.Y - tolerance &&
                   point.Z >= outline.MinimumPoint.Z - tolerance &&
                   point.X <= outline.MaximumPoint.X + tolerance &&
                   point.Y <= outline.MaximumPoint.Y + tolerance &&
                   point.Z <= outline.MaximumPoint.Z + tolerance;
        }

        public static bool OutlineIntersects(Outline outline1, Outline outline2)
        {
            return outline1.MaximumPoint.X > outline2.MinimumPoint.X &&
                   outline1.MinimumPoint.X < outline2.MaximumPoint.X &&
                   outline1.MaximumPoint.Y > outline2.MinimumPoint.Y &&
                   outline1.MinimumPoint.Y < outline2.MaximumPoint.Y &&
                   outline1.MaximumPoint.Z > outline2.MinimumPoint.Z &&
                   outline1.MinimumPoint.Z < outline2.MaximumPoint.Z;
        }

        public static bool IsPointInOutline(XYZ point, Outline outline)
        {
            const double TOLERANCE = 0.01;
            return point.X >= outline.MinimumPoint.X - TOLERANCE &&
                   point.Y >= outline.MinimumPoint.Y - TOLERANCE &&
                   point.Z >= outline.MinimumPoint.Z - TOLERANCE &&
                   point.X <= outline.MaximumPoint.X + TOLERANCE &&
                   point.Y <= outline.MaximumPoint.Y + TOLERANCE &&
                   point.Z <= outline.MaximumPoint.Z + TOLERANCE;
        }

        public static XYZ TransformPoint(XYZ point, Transform transform)
        {
            return transform.OfPoint(point);
        }

        public static Outline GetElementBoundingBoxOutline(Element element)
        {
            BoundingBoxXYZ bbox = element.get_BoundingBox(null);
            if (bbox != null) return new Outline(bbox.Min, bbox.Max);

            // Fallback for elements without bounding boxes
            Options options = new Options();
            GeometryElement geometry = element.get_Geometry(options);
            if (geometry == null) return null;

            XYZ min = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
            XYZ max = new XYZ(double.MinValue, double.MinValue, double.MinValue);

            foreach (GeometryObject obj in geometry)
            {
                if (obj is Solid solid && solid.Volume > 0)
                {
                    BoundingBoxXYZ solidBox = solid.GetBoundingBox();
                    min = new XYZ(
                        Math.Min(min.X, solidBox.Min.X),
                        Math.Min(min.Y, solidBox.Min.Y),
                        Math.Min(min.Z, solidBox.Min.Z));

                    max = new XYZ(
                        Math.Max(max.X, solidBox.Max.X),
                        Math.Max(max.Y, solidBox.Max.Y),
                        Math.Max(max.Z, solidBox.Max.Z));
                }
            }

            return (min.X < max.X) ? new Outline(min, max) : null;
        }

        public static BoundingBoxXYZ GetElementBoundingBox(Element element)
        {

            try
            {
                // First try standard bounding box
                BoundingBoxXYZ bbox = element.get_BoundingBox(null);
                if (bbox != null) return bbox;

                // Fallback: Calculate from solid geometry
                Options options = new Options();
                GeometryElement geom = element.get_Geometry(options);
                if (geom == null) return null;

                BoundingBoxXYZ result = new BoundingBoxXYZ();
                bool initialized = false;


                foreach (GeometryObject obj in geom)
                {
                    if (obj is Solid solid && solid.Volume > 0)
                    {
                        BoundingBoxXYZ solidBox = solid.GetBoundingBox();
                        if (!initialized)
                        {
                            result.Min = solidBox.Min;
                            result.Max = solidBox.Max;
                            initialized = true;
                        }
                        else
                        {
                            result.Min = new XYZ(
                                Math.Min(result.Min.X, solidBox.Min.X),
                                Math.Min(result.Min.Y, solidBox.Min.Y),
                                Math.Min(result.Min.Z, solidBox.Min.Z));

                            result.Max = new XYZ(
                                Math.Max(result.Max.X, solidBox.Max.X),
                                Math.Max(result.Max.Y, solidBox.Max.Y),
                                Math.Max(result.Max.Z, solidBox.Max.Z));
                        }
                    }
                }


                return initialized ? result : null;
            }
            catch
            {
                return null;
            }          
        }
    }
}