namespace CVrpPdTwDynamic.Utils
{
    public class DistanceMatrix
    {
        public static long[,] ComputeEuclideanCostMatrix(in long[,] locations)
        {
            // Calculate the distance matrix using Euclidean distance.
            int locationNumber = locations.GetLength(0);
            long[,] distanceMatrix = new long[locationNumber, locationNumber];
            for (int fromNode = 0; fromNode < locationNumber; fromNode++)
            {
                for (int toNode = 0; toNode < locationNumber; toNode++)
                {
                    if (fromNode == toNode)
                        distanceMatrix[fromNode, toNode] = 0;
                    else
                        distanceMatrix[fromNode, toNode] =
                            (long)Math.Sqrt(Math.Pow(locations[toNode, 0] - locations[fromNode, 0], 2) +
                                            Math.Pow(locations[toNode, 1] - locations[fromNode, 1], 2));
                }
            }
            return distanceMatrix;
        }

        public static long[,] ComputeCostMatrix(in long[,] distanceMatrix, in int[][] Orders, in int n_riders)
        {
            return distanceMatrix;
        }
    }
}