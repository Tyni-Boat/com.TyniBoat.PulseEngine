using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace PulseEngine
{

    public static class PulseDebug
    {
        #region Loggers >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

        /// <summary>
        /// Ecrit un message en console.
        /// </summary>
        /// <param name="_message"></param>
        public static void Log<T>(T _message)
        {
            Debug.Log(_message);
        }

        /// <summary>
        /// Ecrit un message d'alerte en console.
        /// </summary>
        /// <param name="_message"></param>
        public static void LogWarning<T>(T _message)
        {
            Debug.LogWarning(_message);
        }

        /// <summary>
        /// Ecrit un message d'erreur en console.
        /// </summary>
        /// <param name="_message"></param>
        public static void LogError<T>(T _message)
        {
            Debug.LogError(_message);
        }

        #endregion

        #region Graphic >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

        /// <summary>
        /// Dessine une ligne entre A et B.
        /// </summary>
        public static void DrawRLine(Vector3 A, Vector3 B, Color color = default)
        {
            Debug.DrawLine(A, B, color);
        }

        /// <summary>
        /// Dessine une rayon de A dans la direction DIR
        /// </summary>
        public static void DrawRay(Vector3 A, Vector3 Dir, Color color = default)
        {
            Debug.DrawRay(A, Dir, color);
        }

        /// <summary>
        /// Dessine un Cube de centre A , de taille S et de Rotation R
        /// </summary>
        public static void DrawCube(Vector3 A, Vector3 S, Quaternion R, Color color = default)
        {
            R.Normalize();
            Vector3[] cube = new Vector3[8]
            {
            new Vector3(-0.5f, -0.5f, -0.5f),//0
            new Vector3(0.5f, -0.5f, -0.5f),//1
            new Vector3(0.5f, 0.5f, -0.5f),//2
            new Vector3(-0.5f, 0.5f, -0.5f),//3
            new Vector3(-0.5f, 0.5f, 0.5f),//4
            new Vector3(0.5f, 0.5f, 0.5f),//5
            new Vector3(0.5f, -0.5f, 0.5f),//6
            new Vector3(-0.5f, -0.5f, 0.5f),//7
            };
            Vector3[] points = new Vector3[cube.Length];

            for (int i = 0, len = points.Length; i < len; i++)
            {
                var pt = cube[i];
                var trsMatrix = Matrix4x4.TRS(A, R, S);
                points[i] = trsMatrix.MultiplyPoint3x4(pt);
                if (i > 0 && i < len)
                    Debug.DrawLine(points[i], points[i - 1], color);
                if (i >= len - 1)
                {
                    Debug.DrawLine(points[i], points[0], color);
                    Debug.DrawLine(points[i], points[4], color);
                }
                if (i == 3)
                    Debug.DrawLine(points[i], points[0], color);
                if (i == 5)
                    Debug.DrawLine(points[i], points[2], color);
                if (i == 6)
                    Debug.DrawLine(points[i], points[1], color);
            }
        }

        /// <summary>
        /// Draw a capsule shape.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="N"></param>
        /// <param name="startCol"></param>
        /// <param name="endCol"></param>
        /// <param name="tickness"></param>
        /// <param name="showInnerCircles"></param>
        public static void DrawCapsule(Vector3 A, Vector3 B, Quaternion N, float R, Color color = default, int tickness = 30)
        {
            Vector3[] points = null;
            float ABdist = (B - A).magnitude;
            Vector3 normal = (B - A).normalized;
            if (ABdist <= R)
            {
                points = DrawHemisphere(A, R, N, color, tickness, true, true, false, points);
                DrawHemisphere(A, R, N, color, tickness, false, false, false, points);
            }
            else
            {
                bool capReverse = Vector3.Dot(Vector3.up, normal) <= 0;
                Vector3 fwd = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
                Quaternion rot = fwd == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(-fwd);
                points = DrawHemisphere(A, R, rot, color, tickness, true, !capReverse, false, points);
                DrawHemisphere(B, R, rot, color, tickness, false, capReverse, false, points);
            }
        }

        /// <summary>
        /// Dessine une Hemisphere de centre A et de rayon R, aligned with normal N
        /// </summary>
        public static Vector3[] DrawHemisphere(Vector3 A, float R, Vector3 N, Color color = default, int tickness = 30, bool getBaseRing = false, bool reverseCap = false, bool tipLink = false, params Vector3[] linkedCircles)
        {
            int step = 90 / tickness;
            Vector3[] points = linkedCircles;
            Vector3[] basePts = null;
            Vector3 tip = Vector3.zero;
            for (int i = 0, len = step; i <= len; i++)
            {
                float t = (float)i / (step - 1);
                Color col = color;
                Vector3 newCenter = A + (N * (reverseCap ? -1 : 1)) * R * Mathf.Sin((i * tickness) * Mathf.Deg2Rad);
                float newRadius = R * Mathf.Cos((i * tickness) * Mathf.Deg2Rad);
                if (newRadius > 0)
                    points = DrawCircle(newCenter, newRadius, N, col, tickness, true, points);
                if (basePts == null && points != null)
                {
                    basePts = new Vector3[points.Length];
                    points.CopyTo(basePts, 0);
                }
                tip = newCenter;
                if (i >= len)
                {
                    if (points != null)
                    {
                        for (int j = 0; j < points.Length; j++) { Debug.DrawLine(points[j], tip, col); }
                    }
                }
                if (tipLink)
                    Debug.DrawLine(A, tip, color);
            }
            return getBaseRing ? basePts : points;
        }


        /// <summary>
        /// Dessine une Hemisphere de centre A et de rayon R, aligned with normal N
        /// </summary>
        public static Vector3[] DrawHemisphere(Vector3 A, float R, Quaternion N, Color color = default, int tickness = 30, bool getBaseRing = false, bool reverseCap = false, bool tipLink = false, params Vector3[] linkedCircles)
        {
            int step = 90 / tickness;
            Vector3[] points = linkedCircles;
            Vector3[] basePts = null;
            Vector3 tip = Vector3.zero;
            for (int i = 0, len = step; i <= len; i++)
            {
                float t = (float)i / (step - 1);
                Color col = color;
                Vector3 newCenter = A + (N * Vector3.up * (reverseCap ? -1 : 1)) * R * Mathf.Sin((i * tickness) * Mathf.Deg2Rad);
                float newRadius = R * Mathf.Cos((i * tickness) * Mathf.Deg2Rad);
                if (newRadius > 0)
                    points = DrawCircle(newCenter, newRadius, N, col, tickness, true, points);
                if (basePts == null && points != null)
                {
                    basePts = new Vector3[points.Length];
                    points.CopyTo(basePts, 0);
                }
                tip = newCenter;
                if (i >= len)
                {
                    if (points != null)
                    {
                        for (int j = 0; j < points.Length; j++) { Debug.DrawLine(points[j], tip, col); }
                    }
                }
                if (tipLink)
                    Debug.DrawLine(A, tip, color);
            }
            return getBaseRing ? basePts : points;
        }


        /// <summary>
        /// Dessine un cercle de centre A et de rayon R, aligned with normal N
        /// </summary>
        public static Vector3[] DrawCircle(Vector3 A, float R, Quaternion N, Color color = default, int tickness = 30, bool showCircle = false, params Vector3[] linkedCirclePts)
        {
            int step = 360 / tickness;
            Vector3[] points = new Vector3[step];
            for (int i = 0, len = points.Length; i < len; i++)
            {
                float xComp = R * Mathf.Cos((i * tickness) * Mathf.Deg2Rad);
                float yComp = R * Mathf.Sin((i * tickness) * Mathf.Deg2Rad);
                var pt = new Vector3(xComp, 0, yComp);
                var trsMatrix = Matrix4x4.TRS(A, N, Vector3.one);
                points[i] = trsMatrix.MultiplyPoint3x4(pt);
                if ((linkedCirclePts != null && linkedCirclePts.Length != step) || showCircle)
                {
                    if (i > 0 && i < len)
                        Debug.DrawLine(points[i], points[i - 1], color);
                    if (i >= len - 1)
                        Debug.DrawLine(points[i], points[0], color);
                }
                if (linkedCirclePts != null && linkedCirclePts.Length == step)
                {
                    Debug.DrawLine(points[i], linkedCirclePts[i], color);
                }
            }
            return points;
        }


        /// <summary>
        /// Dessine un cercle de centre A et de rayon R, aligned with normal N
        /// </summary>
        public static Vector3[] DrawCircle(Vector3 A, float R, Vector3 N, Color color = default, int tickness = 30, bool showCircle = false, params Vector3[] linkedCirclePts)
        {
            int step = 360 / tickness;
            Vector3[] points = new Vector3[step];
            for (int i = 0, len = points.Length; i < len; i++)
            {
                float xComp = R * Mathf.Cos((i * tickness) * Mathf.Deg2Rad);
                float yComp = R * Mathf.Sin((i * tickness) * Mathf.Deg2Rad);
                var pt = new Vector3(xComp, yComp, 0);
                var trsMatrix = Matrix4x4.TRS(A, Quaternion.LookRotation(N), Vector3.one);
                points[i] = trsMatrix.MultiplyPoint3x4(pt);
                if ((linkedCirclePts != null && linkedCirclePts.Length != step) || showCircle)
                {
                    if (i > 0 && i < len)
                        Debug.DrawLine(points[i], points[i - 1], color);
                    if (i >= len - 1)
                        Debug.DrawLine(points[i], points[0], color);
                }
                if (linkedCirclePts != null && linkedCirclePts.Length == step)
                {
                    Debug.DrawLine(points[i], linkedCirclePts[i], color);
                }
            }
            return points;
        }

        /// <summary>
        /// Dessine un cercle de centre A et de rayon R, aligned with normal N
        /// </summary>
        public static void DrawTwoPointsCurvedCylinder(Vector3 A, Vector3 B, float R, Vector3 Na, Vector3 Nb, Color colorA = default, Color colorB = default
            , int tickness = 30, int subdivisions = 10, bool showSqueleton = false, bool showFlexionPts = false, bool showInnerCircles = false)
        {
            int step = 360 / tickness;
            float innerCircleScalefactor = 1f;

            Vector3 HypoVec = (B - A);
            float scaledHypoMagnitude = HypoVec.magnitude;

            if (scaledHypoMagnitude <= 0)
            {
                DrawCircle(A, R, Na, colorA, tickness);
                return;
            }

            Vector3 AflexionPoint = A + Na.normalized * R * Mathf.Cos(Vector3.Angle(Na.normalized, HypoVec) * Mathf.Deg2Rad);
            Vector3 BflexionPoint = B - Nb.normalized * R * Mathf.Cos(Vector3.Angle(Nb.normalized, HypoVec) * Mathf.Deg2Rad);

            //Draw flex points
            if (showFlexionPts)
            {
                Debug.DrawLine(A, AflexionPoint, colorA);
                Debug.DrawLine(AflexionPoint, BflexionPoint, Color.Lerp(colorA, colorB, 0.5f));
                Debug.DrawLine(BflexionPoint, B, colorB);
            }

            //Draw start point
            Vector3[] lastpoints = null;

            //Dram mid points
            Vector3[] flexPoints = new Vector3[subdivisions];
            for (int i = 0; i < flexPoints.Length; i++)
            {
                float t = ((float)i / (flexPoints.Length - 1));
                Vector3 flexionCurve1 = Vector3.Lerp(Vector3.Lerp(A, AflexionPoint, t), Vector3.Lerp(AflexionPoint, BflexionPoint, t), t);
                Vector3 flexionCurve2 = Vector3.Lerp(Vector3.Lerp(AflexionPoint, BflexionPoint, t), Vector3.Lerp(BflexionPoint, B, t), t);
                flexPoints[i] = Vector3.Lerp(flexionCurve1, flexionCurve2, t);

                var col = Color.Lerp(colorA, colorB, t);
                Vector3 normal = Vector3.Lerp(i > 0 ? (flexPoints[i] - flexPoints[i - 1]).normalized : Na, Vector3.Lerp(Na.normalized, Nb.normalized, t), t);

                //Draw squeleton
                if (showSqueleton)
                {
                    Debug.DrawLine(i > 0 ? flexPoints[i - 1] : A, flexPoints[i], col);
                }

                //Scale of inner rings
                float scaleRadius = R * Mathf.Clamp((t < 0.5f ? (1 - t) : t), innerCircleScalefactor, 1);

                //draw circles
                lastpoints = DrawCircle(flexPoints[i], scaleRadius, normal, col, tickness, showInnerCircles, lastpoints);
            }
        }

        /// <summary>
        /// Draw a path with points
        /// </summary>
        /// <param name="points"></param>
        /// <param name="startColor"></param>
        /// <param name="endColor"></param>
        public static void DrawPath(Vector3[] pts, Color startColor, Color endColor, int pathSubdivisions = 1, float tubeRadius = 0, int tickness = 30, bool showInnerCircles = false, bool showSqueleton = false)
        {
            if (pts == null || pts.Length <= 0)
                return;

            //Add flexion Points
            Vector3[] points = pts;
            for (int i = 0; i < pathSubdivisions; i++)
            {
                points = AddTangentsToPath(points);
            }
            Vector3 lasttravelPt = points[0];

            if (tubeRadius <= 0)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    float t1 = (float)i / (points.Length - 1);
                    Color col = Color.Lerp(startColor, endColor, t1);
                    if (i > 0 && showSqueleton) Debug.DrawLine(points[i - 1], points[i], col);

                    Vector3 travelPt = MultiLerp(points, t1);
                    Debug.DrawLine(lasttravelPt, travelPt, col);
                    lasttravelPt = travelPt;
                }
            }
            else
            {
                Vector3[] lastCirclePts = null;
                for (int i = 0; i < points.Length; i++)
                {
                    float t1 = (float)i / (points.Length - 1);
                    Color col = Color.Lerp(startColor, endColor, t1);

                    Vector3 travelPt = MultiLerp(points, t1);
                    if (showSqueleton)
                    {
                        Debug.DrawLine(lasttravelPt, travelPt, col);
                    }
                    lastCirclePts = DrawCircle(travelPt, tubeRadius, i > 0 ? (travelPt - lasttravelPt).normalized : (points[1] - points[0]).normalized, col, tickness, showInnerCircles, lastCirclePts);
                    lasttravelPt = travelPt;
                }
            }
        }

        /// <summary>
        /// Add Tengents points to a path.
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static Vector3[] AddTangentsToPath(Vector3[] pts)
        {
            List<Vector3> endPoints = new List<Vector3>();
            endPoints.Add(pts[0]);
            for (int i = 1; i < pts.Length; i++)
            {
                //get mid point
                Vector3 midPt = Vector3.Lerp(pts[i - 1], pts[i], 0.5f);
                //get self normal
                Vector3 selftNormal = (pts[i] - pts[i - 1]);
                //get next branch normal
                Vector3 nextNormal = i < pts.Length - 1 ? -(pts[i + 1] - pts[i]) : Vector3.zero;
                //get projected normal
                Vector3 projNormal = Vector3.ProjectOnPlane(nextNormal, selftNormal).normalized;
                //get flexion pt
                Vector3 flexPt = midPt + projNormal * (selftNormal.magnitude * 0.5f) * (1 - 0); //dot here
                                                                                                //add to points
                endPoints.AddRange(new[] { flexPt, pts[i] });
            }
            return endPoints.ToArray();
        }

        /// <summary>
        /// Lerp with multiples points.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Vector3 MultiLerp(Vector3[] points, float f)
        {
            if (points == null || points.Length <= 0)
            {
                Debug.Log("Points list count less than One item or is null");
                return Vector3.zero;
            }
            if (points.Length < 2)
            {
                return points[0];
            }
            if (points.Length == 2)
            {
                return Vector3.Lerp(points[0], points[1], f);
            }
            List<Vector3> lerpPts = new List<Vector3>();
            for (int i = 0; i < points.Length; i++)
            {
                if (i > 0)
                    lerpPts.Add(Vector3.Lerp(points[i - 1], points[i], f));
            }
            Vector3 pt = MultiLerp(lerpPts.ToArray(), f);
            return pt;
        }

        /// <summary>
        /// Subdivise a path
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static List<Vector3> SubdivisePath(Vector3[] points)
        {
            if (points == null || points.Length <= 0)
            {
                Debug.Log("Points list count less than One item or is null");
                return new List<Vector3>();
            }
            var retList = new List<Vector3>();
            for (int i = 1; i < points.Length; i++)
            {
                retList.AddRange(new Vector3[] { points[i - 1], Vector3.Lerp(points[i - 1], points[i], 0.5f), points[i] });
            }
            return retList;
        }

        /// <summary>
        /// Display a colored path
        /// </summary>
        /// <param name="_text"></param>
        public static void DrawPath(Vector3[] _path, Color _startColor = default, Color _endColor = default)
        {
            if (_path == null)
                return;
            for (int i = 0; i < _path.Length - 1; i++)
            {
                Color c = Color.Lerp(_startColor, _endColor, Mathf.InverseLerp(0, _path.Length - 1, i));
                DrawRLine(_path[i], _path[i + 1], c);
            }
        }

        /// <summary>
        /// Affiche du texte en debug
        /// </summary>
        /// <param name="_text"></param>
        public static void DrawText(string _text, float _font, Color _color = default)
        {

        }

        /// <summary>
        /// Transforme un charactere en nuage de points.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static Vector3[] PointsFromChar(char c)
        {
            switch (c.ToString().ToUpper().ToCharArray()[0])
            {
                default:
                    return null;
            }
        }

        #endregion
    }


}