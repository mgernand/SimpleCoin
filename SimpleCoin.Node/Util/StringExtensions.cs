namespace SimpleCoin.Node.Util
{
	using System;
	using System.Collections.Generic;

	public static class StringExtensions
	{
		public static string HexToBinary(this string hexHash)
		{
			string result = string.Empty;

			IDictionary<char, string> lookupTable = new Dictionary<char, string>
			{
				{ '0', "0000" }, { '1', "0001" }, { '2', "0010" }, { '3', "0011" }, { '4', "0100" },
				{ '5', "0101" }, { '6', "0110" }, { '7', "0111" }, { '8', "1000" }, { '9', "1001" },
				{ 'a', "1010" }, { 'b', "1011" }, { 'c', "1100" }, { 'd', "1101" }, { 'e', "1110" },
				{ 'f', "1111" }
			};

			for (int i = 0; i < hexHash.Length; i++)
			{
				char key = hexHash[i];

				if (lookupTable.ContainsKey(key))
				{
					result += lookupTable[key];
				}
				else
				{
					return null;
				}
			}

			return result;
		}

		public static string Times(this string str, int times)
		{
			string prefix = string.Empty;

			for (int i = 0; i < times; i++)
			{
				prefix += str;
			}

			return prefix;
		}
	}
}