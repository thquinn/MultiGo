using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Code {
    class Util {
        public static Collider2D GetMouseCollider(Camera cam, LayerMask layerMask) {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, layerMask);
            if (!hit) {
                return null;
            }
            return hit.collider;
        }

        static string MGG_COOR_ALPHABET = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
        public static string GetMGGCoorFromIndex(int width, int height, int i) {
            int x = i % width, y = height - i / width;
            return GetCoorLetters(x) + y;
        }
        public static string GetCoorLetters(int x) {
            string xString = "";
            do {
                xString = MGG_COOR_ALPHABET[x % MGG_COOR_ALPHABET.Length] + xString;
                x /= MGG_COOR_ALPHABET.Length;
            } while (x > 0);
            return xString;
        }
    }

    public static class ArrayExtensions {
        public static T[] Shuffle<T>(this T[] array) {
            int n = array.Length;
            for (int i = 0; i < n; i++) {
                int r = i + UnityEngine.Random.Range(0, n - i);
                T t = array[r];
                array[r] = array[i];
                array[i] = t;
            }
            return array;
        }
    }
}
