using System.Collections.Generic;

namespace EvieCompilerSystem.Utils
{
    // ReSharper disable once UnusedMember.Global
    public static class MathExtensions {

        /// <summary>
        /// Successive subtraction
        /// </summary>
        public static double ChainDifference(this ICollection<double> collection) {
            double accum = 0;
            var first = true;

            foreach (var v in collection)
            {
                if (first) {
                    accum = v;
                    first = false;
                    continue;
                }
                 
                accum -= v;
            }
            return accum;
        }
        
        /// <summary>
        /// Successive division
        /// </summary>
        public static double ChainDivide(this ICollection<double> collection) {
            double accum = 0;
            var first = true;

            foreach (var v in collection)
            {
                if (first) {
                    accum = v;
                    first = false;
                    continue;
                }
                 
                accum /= v;
            }
            return accum;
        }
        
        /// <summary>
        /// Successive division remainder
        /// </summary>
        public static double ChainRemainder(this ICollection<double> collection) {
            double accum = 0;
            var first = true;

            foreach (var v in collection)
            {
                if (first) {
                    accum = v;
                    first = false;
                    continue;
                }
                 
                accum %= v;
            }
            return accum;
        }

        
        /// <summary>
        /// Successive multiplication
        /// </summary>
        public static double ChainProduct(this ICollection<double> collection) {
            double accum = 1;

            foreach (var v in collection)
            {
                accum *= v;
            }
            return accum;
        }
    }
}