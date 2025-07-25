﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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
                var result = BooleanOperationsUtils.ExecuteBooleanOperation(
                    pipeSolid, slabSolid, BooleanOperationsType.Intersect);

                //TaskDialog.Show("Debug", $"Intersection solid volume: {result?.Volume ?? 0}");
                
                return result;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Debug", $"Boolean operation failed: {ex.Message}");
                return null;
            }
        }

        public static Solid GetElementSolid(Element element, Transform transform = null)
        {
            Options options = new Options { ComputeReferences = true };
            GeometryElement geometry = element.get_Geometry(options);

            if (geometry == null)
            {
            TaskDialog.Show("Debug", $"Element {element.Id} has no geometry");
            return null;
            }


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

            if (solid == null)
            {
                TaskDialog.Show("Debug", $"No valid solid found for element {element.Id}");
            }
            else if (transform != null)
            {
                TaskDialog.Show("Debug", $"Transforming solid for element {element.Id}");
            }


            // Apply coordinate transformation if needed
            if (transform != null)
            {
                solid = SolidUtils.CreateTransformed(solid, transform);
            }

            return solid;
        }

        public static Solid TransformSolid(Solid solid, Transform transform)
        {
            if (transform == null || transform.IsIdentity)
                return solid;

            return SolidUtils.CreateTransformed(solid, transform);
        }

        public static XYZ GetCentroid(Solid solid)
        {
            // More accurate centroid calculation
            XYZ centroid = XYZ.Zero;
            double totalVolume = 0;

            foreach (Face face in solid.Faces)
            {
                Mesh mesh = face.Triangulate();
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    XYZ vertex = mesh.Vertices[i];
                    centroid += vertex;
                }
                totalVolume += mesh.Vertices.Count;
            }

            return totalVolume > 0 ? centroid.Divide(totalVolume) : solid.ComputeCentroid();
        }


        // 0 referencias
        //public static Curve GetPipeCenterline(Element pipe)
        //{
        //    if (pipe.Location is LocationCurve curveLoc)
        //    {
        //        return curveLoc.Curve;
        //    }
        //    return null;
        //}
        //// 0 referencias
        //public static bool IsElementInView(Element e, View view)
        //{
        //    try
        //    {
        //        return !e.IsHidden(view);
        //    }
        //    catch
        //    {
        //        return true; // Fallback
        //    }
        //}
        //// 0 referencias
        //public static bool OutlineContains(Outline outline, XYZ point, double tolerance = 0.001)
        //{
        //    return point.X >= outline.MinimumPoint.X - tolerance &&
        //           point.Y >= outline.MinimumPoint.Y - tolerance &&
        //           point.Z >= outline.MinimumPoint.Z - tolerance &&
        //           point.X <= outline.MaximumPoint.X + tolerance &&
        //           point.Y <= outline.MaximumPoint.Y + tolerance &&
        //           point.Z <= outline.MaximumPoint.Z + tolerance;
        //}
        //// 0 referencias
        //public static bool OutlineIntersects(Outline outline1, Outline outline2)
        //{
        //    return outline1.MaximumPoint.X > outline2.MinimumPoint.X &&
        //           outline1.MinimumPoint.X < outline2.MaximumPoint.X &&
        //           outline1.MaximumPoint.Y > outline2.MinimumPoint.Y &&
        //           outline1.MinimumPoint.Y < outline2.MaximumPoint.Y &&
        //           outline1.MaximumPoint.Z > outline2.MinimumPoint.Z &&
        //           outline1.MinimumPoint.Z < outline2.MaximumPoint.Z;
        //}
        //// 0 referencias
        //public static bool IsPointInOutline(XYZ point, Outline outline)
        //{
        //    const double TOLERANCE = 0.01;
        //    return point.X >= outline.MinimumPoint.X - TOLERANCE &&
        //           point.Y >= outline.MinimumPoint.Y - TOLERANCE &&
        //           point.Z >= outline.MinimumPoint.Z - TOLERANCE &&
        //           point.X <= outline.MaximumPoint.X + TOLERANCE &&
        //           point.Y <= outline.MaximumPoint.Y + TOLERANCE &&
        //           point.Z <= outline.MaximumPoint.Z + TOLERANCE;
        //}

        //// 0 referencias
        //public static XYZ TransformPoint(XYZ point, Transform transform)
        //{
        //    return transform.OfPoint(point);
        //}

        //// 0 referencias
        //public static Outline GetElementBoundingBoxOutline(Element element)
        //{
        //    try
        //    {
        //        BoundingBoxXYZ bbox = element.get_BoundingBox(null);
        //        if (bbox != null) return new Outline(bbox.Min, bbox.Max);

        //        Options options = new Options();
        //        GeometryElement geometry = element.get_Geometry(options);
        //        if (geometry == null) return null;

        //        XYZ min = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
        //        XYZ max = new XYZ(double.MinValue, double.MinValue, double.MinValue);

        //        foreach (GeometryObject obj in geometry)
        //        {
        //            if (obj is Solid solid && solid.Volume > 0)
        //            {
        //                min = new XYZ(
        //                    Math.Min(min.X, solid.GetBoundingBox().Min.X),
        //                    Math.Min(min.Y, solid.GetBoundingBox().Min.Y),
        //                    Math.Min(min.Z, solid.GetBoundingBox().Min.Z)
        //                );
        //                max = new XYZ(
        //                    Math.Max(max.X, solid.GetBoundingBox().Max.X),
        //                    Math.Max(max.Y, solid.GetBoundingBox().Max.Y),
        //                    Math.Max(max.Z, solid.GetBoundingBox().Max.Z)
        //                );
        //            }
        //        }

        //        return (min.X < max.X) ? new Outline(min, max) : null;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
        //// 0 referencias
        //public static BoundingBoxXYZ GetElementBoundingBox(Element element)
        //{

        //    try
        //    {
        //        // First try standard bounding box
        //        BoundingBoxXYZ bbox = element.get_BoundingBox(null);
        //        if (bbox != null) return bbox;

        //        // Fallback: Calculate from solid geometry
        //        Options options = new Options();
        //        GeometryElement geom = element.get_Geometry(options);
        //        if (geom == null) return null;

        //        BoundingBoxXYZ result = new BoundingBoxXYZ();
        //        bool initialized = false;


        //        foreach (GeometryObject obj in geom)
        //        {
        //            if (obj is Solid solid && solid.Volume > 0)
        //            {
        //                BoundingBoxXYZ solidBox = solid.GetBoundingBox();
        //                if (!initialized)
        //                {
        //                    result.Min = solidBox.Min;
        //                    result.Max = solidBox.Max;
        //                    initialized = true;
        //                }
        //                else
        //                {
        //                    result.Min = new XYZ(
        //                        Math.Min(result.Min.X, solidBox.Min.X),
        //                        Math.Min(result.Min.Y, solidBox.Min.Y),
        //                        Math.Min(result.Min.Z, solidBox.Min.Z));

        //                    result.Max = new XYZ(
        //                        Math.Max(result.Max.X, solidBox.Max.X),
        //                        Math.Max(result.Max.Y, solidBox.Max.Y),
        //                        Math.Max(result.Max.Z, solidBox.Max.Z));
        //                }
        //            }
        //        }


        //        return initialized ? result : null;
        //    }
        //    catch
        //    {
        //        return null;
        //    }          
        //}
    }
}