using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace PulseEngine
{

    public static class PulseExtensions
    {
        #region Constants #############################################################

        #endregion

        #region Variables #############################################################

        #endregion

        #region Statics   #############################################################

        #endregion

        #region Inner Types ###########################################################

        #endregion

        #region Properties ############################################################

        #endregion

        #region Public Functions ######################################################

        /// <summary>
        /// Check if an item meet condition and return his index in the collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool IndexOfItem<T>(this T[] collection, Predicate<T> condition, out int index)
        {
            if (collection == null || collection.Length <= 0)
            {
                index = -1;
                return false;
            }
            index = collection.FindIndex(condition);
            return index >= 0;
        }

        /// <summary>
        /// Check if an item meet condition and return his index in the collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool IndexOfItem<T>(this List<T> collection, Predicate<T> condition, out int index)
        {
            if (collection == null || collection.Count <= 0)
            {
                index = -1;
                return false;
            }
            index = collection.FindIndex(condition);
            return index >= 0;
        }

        /// <summary>
        /// Check if an index is not out of range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool IsInRange<T>(this T[] collection, int index) => collection != null && index >= 0 && index < collection.Length;

        /// <summary>
        /// Check if an index is not out of range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool IsInRange<T>(this List<T> collection, int index) => collection != null && index >= 0 && index < collection.Count;

        /// <summary>
        /// Return a collection of Type T based on a collection of type Q
        /// </summary>
        /// <typeparam name="Q"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static T[] CollectionOf<Q, T>(this Q[] collection, Func<Q, T> selector)
        {
            if (collection == null || selector == null)
                return null;
            T[] retCol = new T[collection.Length];
            for (int i = 0; i < collection.Length; i++)
            {
                retCol[i] = selector.Invoke(collection[i]);
            }
            return retCol;
        }

        /// <summary>
        /// Return a collection of Type T based on a collection of type Q
        /// </summary>
        /// <typeparam name="Q"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static List<T> CollectionOf<Q, T>(this List<Q> collection, Func<Q, T> selector)
        {
            if (collection == null || selector == null)
                return null;
            List<T> retCol = new List<T>();
            for (int i = 0; i < collection.Count; i++)
            {
                retCol.Add(selector.Invoke(collection[i]));
            }
            return retCol;
        }

        /// <summary>
        /// Is this object in the interval formed by min and max?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="min">the minimal value</param>
        /// <param name="max">the maximum value</param>
        /// <param name="openMin">min comparator is open?</param>
        /// <param name="openMax">max comparator is open?</param>
        /// <returns></returns>
        public static bool InInterval<T>(this T obj, T min, T max, bool openMin = false, bool openMax = true) where T : IComparable<T>
        {
            bool minConditionMeet = openMin ? obj.CompareTo(min) > 0 : obj.CompareTo(min) >= 0;
            bool maxConditionMeet = openMax ? obj.CompareTo(max) < 0 : obj.CompareTo(max) <= 0;
            return minConditionMeet && maxConditionMeet;
        }

        /// <summary>
        /// Spread the value in parts on an min-max interval and get the value at a part index
        /// </summary>
        /// <param name="value">the value to spread</param>
        /// <param name="parts">the number of parts</param>
        /// <param name="index">the desired part index</param>
        /// <param name="minWeight">the minimum value</param>
        /// <param name="maxWeight">the maximum value</param>
        /// <returns></returns>
        public static float SpreadEvenly(this float value, int parts, int index, float minWeight = 0, float maxWeight = 1)
        {
            if (index < 0 || index >= parts)
                return 0;
            float outputValue = 0;
            int previousIndex = Mathf.Clamp(index - 1, 0, parts - 1);
            int nextIndex = Mathf.Clamp(index + 1, 0, parts - 1);
            float clampedWeight = Mathf.Clamp(value, minWeight, maxWeight);
            float totalWeightInterval = maxWeight - minWeight;
            float singlePartInterval = totalWeightInterval / parts;
            float thisPartMin = minWeight + previousIndex * singlePartInterval;
            float thisPartMax = maxWeight - singlePartInterval * ((parts - 1) - nextIndex);
            float thisPartPeak = index >= (parts - 1)
                ? thisPartMax
                : (index <= 0
                    ? thisPartMin
                    : (thisPartMax - thisPartMin) * 0.5f + thisPartMin);
            if (clampedWeight.InInterval(thisPartMin, thisPartPeak))
            {
                outputValue = Mathf.InverseLerp(thisPartMin, thisPartPeak, clampedWeight);
                return index <= 0 ? 1 : outputValue;
            }
            else if (clampedWeight.InInterval(thisPartPeak, thisPartMax, false, false))
            {
                outputValue = 1 - Mathf.InverseLerp(thisPartPeak, thisPartMax, clampedWeight);
                return index >= (parts - 1) ? 1 : outputValue;
            }
            return outputValue;
        }


        /// <summary>
        /// Lis dans un champs prive de la data ayant l'attribut [serializeField]
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static object ReadField<Q>(this Q obj, string fieldName)
        {
            if (obj == null)
                return default;
            if (string.IsNullOrEmpty(fieldName))
                return default;
            var fields = GetFieldInside(typeof(Q), fieldName);
            if (fields == null)
                return default;
            if (fields.GetCustomAttribute<SerializeField>() == null)
                return default;
            return fields.GetValue(obj);
        }

        /// <summary>
        /// Ecrit dans un champs prive de la data ayant l'attribut [serializeField]
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static bool WriteField<Q>(this Q obj, string fieldName, object value)
        {
            if (obj == null)
                return false;
            if (string.IsNullOrEmpty(fieldName))
                return false;
            var fields = GetFieldInside(typeof(Q), fieldName);
            if (fields == null)
                return false;
            if (fields.GetCustomAttribute<SerializeField>() == null)
                return false;
            fields.SetValue(obj, value);
            return true;
        }

        /// <summary>
        /// Clear and array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        public static void Clear<T>(this T[] collection)
        {
            if (collection == null)
                return;
            for (int i = 0; i < collection.Length; i++)
            {
                collection[i] = default(T);
            }
        }


        /// <summary>
        /// Return the first index in the array meeting the condition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static int FindIndex<T>(this T[] collection, Predicate<T> condition)
        {
            if (collection == null || condition == null)
                return -1;
            for (int i = 0; i < collection.Length; i++)
            {
                if (condition(collection[i]))
                    return i;
            }

            return -1;
        }



        /// <summary>
        /// Get the Closest point from the "point" on the collider surface.
        /// </summary>
        /// <param name="collider"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Vector3 PointOnSurface(this Collider collider, Vector3 point)
        {
            if (!collider)
                return point;

            if (collider is SphereCollider)
            {
                Vector3 p = point;

                //Get the direction
                p = point - collider.transform.position;
                p.Normalize();

                //Clamp on surface
                p *= (collider as SphereCollider).radius * collider.transform.localScale.x;
                p += collider.transform.position;

                return p;
            }
            else if (collider is BoxCollider)
            {
                var box = (collider as BoxCollider);
                // Cache the collider transform
                var ct = collider.transform;

                // Firstly, transform the point into the space of the collider
                var local = ct.InverseTransformPoint(point);

                //get the point outside the collider if is inside
                if (local.x.InInterval(-box.size.x * 0.5f, box.size.x * 0.5f)
                    && local.y.InInterval(-box.size.y * 0.5f, box.size.y * 0.5f)
                    && local.z.InInterval(-box.size.z * 0.5f, box.size.z * 0.5f))
                {
                    Vector3 boxWc = ct.TransformPoint(box.center);
                    Vector3 virtualPt = boxWc + (point - boxWc).normalized * box.size.sqrMagnitude * 2;
                    local = ct.InverseTransformPoint(virtualPt);
                }

                // Now, shift it to be in the center of the box
                local -= box.center;

                // Inverse scale it by the colliders scale
                var localNorm =
                new Vector3(
                Mathf.Clamp(local.x, -box.size.x * 0.5f, box.size.x * 0.5f),
                Mathf.Clamp(local.y, -box.size.y * 0.5f, box.size.y * 0.5f),
                Mathf.Clamp(local.z, -box.size.z * 0.5f, box.size.z * 0.5f)
                );

                // Now we undo our transformations
                localNorm += (collider as BoxCollider).center;

                // Return resulting point
                return ct.TransformPoint(localNorm);
            }
            else if (collider is MeshCollider)
            {
                //get the mesh
                var mesh = (collider as MeshCollider).sharedMesh;
                Transform ct = collider.transform;
                int verticeIndex = -1;

                //get the closest vertice
                List<Vector3> vertices = new List<Vector3>(mesh.vertices);
                for (int i = 0; i < vertices.Count; i++)
                {
                    vertices[i] = ct.TransformPoint(vertices[i]);
                }
                vertices.Sort((v1, v2) => { return (point - v1).sqrMagnitude.CompareTo((point - v2).sqrMagnitude); });
                verticeIndex = mesh.vertices.FindIndex(v => ct.InverseTransformPoint(vertices[0]) == v);

                Vector3 transformedVerticePos = ct.TransformPoint(mesh.vertices[verticeIndex]);

                //get the closest vertice's normal
                if (mesh.normals.IsInRange(verticeIndex))
                {
                    //get the direction and project it on normal
                    Vector3 normal = mesh.normals[verticeIndex];
                    Vector3 p = Vector3.ProjectOnPlane((point - transformedVerticePos), normal);
                    return transformedVerticePos + p;
                }
                else
                {
                    return transformedVerticePos;
                }
            }
            else if (collider is TerrainCollider)
            {
                var ter = collider as TerrainCollider;
                //ter.terrainData.
                //PulseDebug.DrawRLine(point, new Vector3(point.x, ter.terrainData.hei, point.z), Color.magenta);
                float maxHeightDiff = 100;
                Vector3 dir = (point - collider.transform.position);
                Vector3 proj = Vector3.ProjectOnPlane(dir, collider.transform.up);
                Ray r = new Ray(point, ((collider.transform.position + proj) - point).normalized);
                collider.Raycast(r, out var hit, maxHeightDiff);
                return hit.point;
            }
            else if (collider is CapsuleCollider)
            {
                CapsuleCollider capsule = (CapsuleCollider)collider;

                Vector3 shapeDir = capsule.transform.up;
                Vector3 center = capsule.transform.TransformPoint(capsule.center);
                Vector3 capsuleSecondaryCenter = center + shapeDir * (capsule.height * 0.5f - capsule.radius);
                Vector3 capsulePrimaryCenter = center - shapeDir * (capsule.height * 0.5f - capsule.radius);
                float innerHeight = (capsule.height - (capsule.radius * 2)) * 0.5f;
                Vector3 dir = (point - center);
                Vector3 heightProj = Vector3.Project(dir, shapeDir);
                //in the hemispheres
                if (heightProj.magnitude > innerHeight)
                {
                    Vector3 hemiSphereCenter = Vector3.Dot(heightProj.normalized, shapeDir) > 0 ? capsuleSecondaryCenter : capsulePrimaryCenter;
                    Vector3 hDir = (point - hemiSphereCenter);
                    return hemiSphereCenter + hDir.normalized * capsule.radius;
                }
                //in the cylinder
                else
                {
                    return center + heightProj + (Vector3.ProjectOnPlane(dir, shapeDir).normalized * capsule.radius);
                }
            }
            else if (collider is CharacterController)
            {
                CharacterController character = (CharacterController)collider;

                Vector3 shapeDir = character.transform.up;
                Vector3 center = character.transform.TransformPoint(character.center);
                Vector3 capsuleSecondaryCenter = center + shapeDir * (character.height * 0.5f - character.radius);
                Vector3 capsulePrimaryCenter = center - shapeDir * (character.height * 0.5f - character.radius);
                float innerHeight = (character.height - (character.radius * 2)) * 0.5f;
                Vector3 dir = (point - center);
                Vector3 heightProj = Vector3.Project(dir, shapeDir);
                //in the hemispheres
                if (heightProj.magnitude > innerHeight)
                {
                    Vector3 hemiSphereCenter = Vector3.Dot(heightProj.normalized, shapeDir) > 0 ? capsuleSecondaryCenter : capsulePrimaryCenter;
                    Vector3 hDir = (point - hemiSphereCenter);
                    return hemiSphereCenter + hDir.normalized * character.radius;
                }
                //in the cylinder
                else
                {
                    return center + heightProj + (Vector3.ProjectOnPlane(dir, shapeDir).normalized * character.radius);
                }
            }

            return point;
        }

        /// <summary>
        /// Get the width of a collider.
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public static float GetColliderWidth(this Collider collider)
        {
            if (!collider)
                return 0;

            if (collider is SphereCollider)
            {
                return (collider as SphereCollider).radius;
            }
            else if (collider is BoxCollider)
            {
                var box = (collider as BoxCollider);
                return Mathf.Sqrt((box.size.x * box.size.x) + (box.size.z * box.size.z));
            }
            else if (collider is MeshCollider)
            {
                //get the mesh
                var mesh = (collider as MeshCollider).sharedMesh;
                return Mathf.Sqrt((mesh.bounds.size.x * mesh.bounds.size.x) + (mesh.bounds.size.z * mesh.bounds.size.z));
            }
            else if (collider is TerrainCollider)
            {
                var ter = collider as TerrainCollider;
                var size = ter.terrainData.size;
                return Mathf.Sqrt((size.x * size.x) + (size.z * size.z));
            }
            else if (collider is CapsuleCollider)
            {
                CapsuleCollider capsule = (CapsuleCollider)collider;
                return capsule.radius;
            }
            else if (collider is CharacterController)
            {
                CharacterController character = (CharacterController)collider;
                return character.radius;
            }
            return 0;
        }

        /// <summary>
        /// Get the height of a collider
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public static float GetColliderHeight(this Collider collider)
        {
            if (!collider)
                return 0;

            if (collider is SphereCollider)
            {
                return (collider as SphereCollider).radius;
            }
            else if (collider is BoxCollider)
            {
                var box = (collider as BoxCollider);
                return box.size.y;
            }
            else if (collider is MeshCollider)
            {
                //get the mesh
                var mesh = (collider as MeshCollider).sharedMesh;
                return mesh.bounds.size.y;
            }
            else if (collider is TerrainCollider)
            {
                var ter = collider as TerrainCollider;
                var bounds = ter.bounds;
                return bounds.size.y;
            }
            else if (collider is CapsuleCollider)
            {
                CapsuleCollider capsule = (CapsuleCollider)collider;
                return capsule.height;
            }
            else if (collider is CharacterController)
            {
                CharacterController character = (CharacterController)collider;
                return character.height;
            }
            return 0;
        }

        /// <summary>
        /// Get the center of the collider in local space.
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public static Vector3 GetColliderCenter(this Collider collider)
        {
            if (!collider)
                return Vector3.zero;

            if (collider is SphereCollider)
            {
                return (collider as SphereCollider).center;
            }
            else if (collider is BoxCollider)
            {
                var box = (collider as BoxCollider);
                return box.center;
            }
            else if (collider is MeshCollider)
            {
                //get the mesh
                var mesh = (collider as MeshCollider).sharedMesh;
                return mesh.bounds.center;
            }
            else if (collider is TerrainCollider)
            {
                var ter = collider as TerrainCollider;
                return ter.bounds.center;
            }
            else if (collider is CapsuleCollider)
            {
                CapsuleCollider capsule = (CapsuleCollider)collider;
                return capsule.center;
            }
            else if (collider is CharacterController)
            {
                CharacterController character = (CharacterController)collider;
                return character.center;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Set the width of a collider.
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public static void SetColliderWidth(this Collider collider, float value)
        {
            if (!collider)
                return;

            if (collider is SphereCollider)
            {
                (collider as SphereCollider).radius = value;
            }
            else if (collider is BoxCollider)
            {
                var box = (collider as BoxCollider);
                box.size = new Vector3(value * Mathf.Sqrt(2), box.size.y, value * Mathf.Sqrt(2));
            }
            else if (collider is MeshCollider)
            {
                //get the mesh
                var mesh = (collider as MeshCollider).sharedMesh;
                return;
            }
            else if (collider is TerrainCollider)
            {
                var ter = collider as TerrainCollider;
                var size = ter.terrainData.size;
                return;
            }
            else if (collider is CapsuleCollider)
            {
                CapsuleCollider capsule = (CapsuleCollider)collider;
                capsule.radius = value;
            }
            else if (collider is CharacterController)
            {
                CharacterController character = (CharacterController)collider;
                character.radius = value;
            }
        }

        /// <summary>
        /// Set the height of a collider
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public static void SetColliderHeight(this Collider collider, float value)
        {
            if (!collider)
                return;

            if (collider is SphereCollider)
            {
                return;
            }
            else if (collider is BoxCollider)
            {
                var box = (collider as BoxCollider);
                box.size = new Vector3(box.size.x, value, box.size.z);
            }
            else if (collider is MeshCollider)
            {
                var mesh = (collider as MeshCollider).sharedMesh;
                return;
            }
            else if (collider is TerrainCollider)
            {
                var ter = collider as TerrainCollider;
                var size = ter.terrainData.size;
                return;
            }
            else if (collider is CapsuleCollider)
            {
                CapsuleCollider capsule = (CapsuleCollider)collider;
                capsule.height = value;
            }
            else if (collider is CharacterController)
            {
                CharacterController character = (CharacterController)collider;
                character.height = value;
            }
        }

        /// <summary>
        /// Set the center of the collider in local space.
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public static void SetColliderCenter(this Collider collider, Vector3 value)
        {
            if (!collider)
                return;

            if (collider is SphereCollider)
            {
                (collider as SphereCollider).center = value;
            }
            else if (collider is BoxCollider)
            {
                var box = (collider as BoxCollider);
                box.center = value;
            }
            else if (collider is MeshCollider)
            {
                var mesh = (collider as MeshCollider).sharedMesh;
                return;
            }
            else if (collider is TerrainCollider)
            {
                var ter = collider as TerrainCollider;
                return;
            }
            else if (collider is CapsuleCollider)
            {
                CapsuleCollider capsule = (CapsuleCollider)collider;
                capsule.center = value;
            }
            else if (collider is CharacterController)
            {
                CharacterController character = (CharacterController)collider;
                character.center = value;
            }
        }


        /// <summary>
        /// Serialize a Resource to Json format
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToJson<T>(this T obj) where T : ScriptableResource { return ""; }

        /// <summary>
        /// Serialize a Resource to Binary Byte Array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] ToByteArray<T>(this T obj) where T : ScriptableResource { return default; }

        /// <summary>
        /// Get the angle calculated from horizontal axis.
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="verticalAxis"></param>
        /// <returns></returns>
        public static float AngleFromHorizontal(this RaycastHit hit, Vector3 verticalAxis)
        {
            return Mathf.Acos(Vector3.Dot(verticalAxis.normalized, hit.normal.normalized)) * Mathf.Rad2Deg;
        }

        #endregion

        #region Private Functions #####################################################

        /// <summary>
        /// Get a fied recursively in base classes.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private static FieldInfo GetFieldInside(Type type, string fieldName)
        {
            FieldInfo fi = null;
            fi = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            var baseClass = type.BaseType;
            if (fi == null && baseClass != null)
            {
                fi = GetFieldInside(baseClass, fieldName);
            }
            return fi;
        }

        #endregion

    }


}