﻿using System.IO;
using System.Linq;
using Geo.Abstractions.Interfaces;
using Geo.Geometries;

namespace Geo.IO.Wkb
{
    public class WkbWriter
    {
        private readonly WkbWriterSettings _settings;

        public WkbWriter()
        {
            _settings = new WkbWriterSettings();
        }

        public WkbWriter(WkbWriterSettings settings)
        {
            _settings = settings;
        }

        public byte[] Write(IOgcGeometry geometry)
        {
            using (var stream = new MemoryStream())
            {
                Write(geometry, stream);
                return stream.ToArray();
            }
        }

        public void Write(IOgcGeometry geometry, Stream stream)
        {
            using (var writer = new WkbBinaryWriter(stream, _settings.Encoding))
            {
                WriteEncoding(writer, _settings.Encoding);
                Write(geometry, writer);
            }
        }

        private void WriteEncoding(WkbBinaryWriter writer, WkbEncoding encoding)
        {
            writer.Write((byte)encoding);
        }

        private void Write(IGeometry geometry, WkbBinaryWriter writer)
        {
            var point = geometry as Point;
            if (point != null)
                WritePoint(point, writer);

            var lineString = geometry as LineString;
            if (lineString != null)
                WriteLineString(lineString, writer);

            if (_settings.Triangle)
            {
                var triangle = geometry as Triangle;
                if (triangle != null)
                    WriteTriangle(triangle, writer);
            }

            var polygon = geometry as Polygon;
            if (polygon != null)
                WritePolygon(polygon, writer);

            var multiPoint = geometry as MultiPoint;
            if (multiPoint != null)
                WriteMultiPoint(multiPoint, writer);

            var multiLineString = geometry as MultiLineString;
            if (multiLineString != null)
                WriteMultiLineString(multiLineString, writer);

            var multiPolygon = geometry as MultiPolygon;
            if (multiPolygon != null)
                WriteMultiPolygon(multiPolygon, writer);

            var geometryCollection = geometry as GeometryCollection;
            if (geometryCollection != null)
                WriteGeometryCollection(geometryCollection, writer);
        }

        private void WriteCoordinate(Coordinate coordinate, WkbBinaryWriter writer)
        {
            writer.Write(coordinate.Longitude);
            writer.Write(coordinate.Latitude);

            if (coordinate.Is3D)
                writer.Write(coordinate.Elevation);

            if (coordinate.IsMeasured)
                writer.Write(coordinate.M);
        }

        private void WriteCoordinates(CoordinateSequence coordinates, WkbBinaryWriter writer)
        {
            writer.Write((uint)coordinates.Count);

            foreach (var coordinate in coordinates)
                WriteCoordinate(coordinate, writer);
        }

        private void WritePoint(Point point, WkbBinaryWriter writer)
        {
            if (!point.IsEmpty)
            {
                WriteGeometryType(point, WkbGeometryType.Point, writer);
                WriteCoordinate(point.Coordinate, writer);
            }
        }

        private void WriteLineString(LineString lineString, WkbBinaryWriter writer)
        {
            WriteGeometryType(lineString, WkbGeometryType.LineString, writer);
            WriteCoordinates(lineString.Coordinates, writer);
        }

        private void WritePolygon(Polygon polygon, WkbBinaryWriter writer)
        {
            WriteGeometryType(polygon, WkbGeometryType.Polygon, writer);
            WritePolygonInner(polygon, writer);
        }

        private void WriteTriangle(Triangle triangle, WkbBinaryWriter writer)
        {
            WriteGeometryType(triangle, WkbGeometryType.Triangle, writer);
            WritePolygonInner(triangle, writer);
        }

        private void WritePolygonInner(Polygon polygon, WkbBinaryWriter writer)
        {
            if (polygon.IsEmpty)
            {
                writer.Write(0u);
            }
            else
            {
                writer.Write((uint)(1 + polygon.Holes.Count));
                WriteCoordinates(polygon.Shell.Coordinates, writer);

                foreach (var hole in polygon.Holes)
                    WriteCoordinates(hole.Coordinates, writer);
            }
        }

        private void WriteMultiPoint(MultiPoint multipoint, WkbBinaryWriter writer)
        {
            WriteGeometryType(multipoint, WkbGeometryType.MultiPoint, writer);
            var points = multipoint.Geometries.Cast<Point>().Where(x => !x.IsEmpty).ToList();
            writer.Write((uint)points.Count);
            foreach (var point in points)
                WriteCoordinate(point.Coordinate, writer);
        }

        private void WriteMultiLineString(MultiLineString multiLineString, WkbBinaryWriter writer)
        {
            WriteGeometryType(multiLineString, WkbGeometryType.MultiLineString, writer);
            writer.Write((uint)multiLineString.Geometries.Count);
            foreach (var linestring in multiLineString.Geometries.Cast<LineString>())
                WriteCoordinates(linestring.Coordinates, writer);
        }

        private void WriteMultiPolygon(MultiPolygon multiPolygon, WkbBinaryWriter writer)
        {
            WriteGeometryType(multiPolygon, WkbGeometryType.MultiPolygon, writer);
            writer.Write((uint)multiPolygon.Geometries.Count);
            foreach (var polygon in multiPolygon.Geometries.Cast<Polygon>())
                WritePolygonInner(polygon, writer);
        }

        private void WriteGeometryCollection(GeometryCollection collection, WkbBinaryWriter writer)
        {
            WriteGeometryType(collection, WkbGeometryType.GeometryCollection, writer);
            var geometries = collection.Geometries.Where(x => !(x is Point) || !x.IsEmpty).ToList();
            writer.Write((uint)geometries.Count);
            foreach (var geometry in geometries)
                Write(geometry, writer);
        }

        private void WriteGeometryType(IOgcGeometry geometry, WkbGeometryType baseType, WkbBinaryWriter writer)
        {
            if (geometry.IsEmpty)
            {
                writer.Write((uint)baseType);
            }
            else
            {
                var typeCode = (uint)baseType;

                if (geometry.Is3D)
                    typeCode += 1000;

                if (geometry.IsMeasured)
                    typeCode += 2000;

                writer.Write(typeCode);
            }
        }
    }
}
