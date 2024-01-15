public static class StringHelper
{
	//Returns true if a equals b else false.
	public static bool Matches(char a, char b)
	{
		return a == b;
	}
	//Returns true if a equals b.
	public static bool Matches(string a, string b)
	{
		if (a is null)
		{
			a = "";
		}
		if (b is null)
		{
			b = "";
		}
		if (a.Length != b.Length)
		{
			return false;
		}
		for (int i = 0; i < a.Length; i++)
		{
			if (!Matches(a[i], b[i]))
			{
				return false;
			}
		}
		return true;
	}
	//Returns true if a contains b else false.
	public static bool Contains(string a, char b)
	{
		if (a is null)
		{
			return false;
		}
		for (int i = 0; i < a.Length; i++)
		{
			if (Matches(a[i], b))
			{
				return true;
			}
		}
		return false;
	}
	//Returns true if a contains b else false.
	public static bool Contains(string a, string b)
	{
		if (a is null && b is null)
		{
			return true;
		}
		if (a is null || b is null)
		{
			return false;
		}
		if (b.Length > a.Length)
		{
			return false;
		}
		for (int i = 0; i < a.Length; i++)
		{
			if (Matches(a.Substring(i, b.Length), b))
			{
				return true;
			}
		}
		return false;
	}
	//Replaces every instance of b within a with c.
	public static string Replace(string a, string b, string c)
	{
		if (a is null)
		{
			return "";
		}
		if (b is null || b is "")
		{
			return a;
		}
		if (c is null)
		{
			c = "";
		}
		if (Contains(c, b))
		{
			throw new System.Exception("c cannot contain b.");
		}
		for (int i = 0; i < a.Length - b.Length; i++)
		{
			string t = a.Substring(i, i + b.Length - 1);
			if (Matches(t, b))
			{
				var d = a.Substring(0, i - 1);
				var e = a.Substring(i + b.Length - 1, a.Length - 1);
				a = d + c + e;
			}
		}
		return a;
	}
	//Returns the index of the first character of b within the first occurance of b within a or -1 if a does not contain b.
	public static int FirstIndexOf(string a, string b)
	{
		if (a is null || b is null)
		{
			return -1;
		}
		for (int i = 0; i < a.Length - b.Length; i++)
		{
			if (Matches(a.Substring(i, b.Length), b))
			{
				return i;
			}
		}
		return -1;
	}
	//Returns the index of the first character of b within the first occurance of b within a or -1 if a does not contain b.
	public static int LastIndexOf(string a, string b)
	{
		if (a is null || b is null)
		{
			return -1;
		}
		for (int i = a.Length - b.Length; i >= 0; i--)
		{
			if (Matches(a.Substring(i, b.Length), b))
			{
				return i;
			}
		}
		return -1;
	}
	//Returns everything after the first occurance of b from a.
	public static string SelectBeforeFirst(string a, string b)
	{
		if (a is null || b is null)
		{
			return "";
		}
		int index = FirstIndexOf(a, b);
		if (index is -1)
		{
			return "";
		}
		return a.Substring(0, index);
	}
	//Returns everything after the first occurance of b from a.
	public static string SelectAfterFirst(string a, string b)
	{
		if (a is null || b is null)
		{
			return "";
		}
		int index = FirstIndexOf(a, b);
		if (index is -1)
		{
			return "";
		}
		return a.Substring(index + b.Length, a.Length - (index + b.Length));
	}
	//Returns everything after the first occurance of b from a.
	public static string SelectBeforeLast(string a, string b)
	{
		if (a is null || b is null)
		{
			return "";
		}
		int index = LastIndexOf(a, b);
		if (index is -1)
		{
			return "";
		}
		return a.Substring(0, index);
	}
	//Returns everything after the first occurance of b from a.
	public static string SelectAfterLast(string a, string b)
	{
		if (a is null || b is null)
		{
			return "";
		}
		int index = LastIndexOf(a, b);
		if (index is -1)
		{
			return "";
		}
		return a.Substring(index + b.Length, a.Length - (index + b.Length));
	}
	//Replaces CRLF with LF in the specified document and deletes lone CR characters.
	public static string FixLineEndings(string a)
	{
		return a.Replace("\r\n", "\n").Replace("\r", "");
	}
	//Removes spaces, tabs, new lines, and line feeds from the start of a.
	public static string TrimLeadingWhitespace(string a)
	{
		for (int i = 0; i < a.Length; i++)
		{
			if (a[i] != ' ' && a[i] != '\n' && a[i] != '\r' && a[i] != '\t' && a[i] != '\0')
			{
				return a.Substring(i, a.Length - i);
			}
		}
		return "";
	}
	//Removes spaces, tabs, new lines, and line feeds from the end of a.
	public static string TrimTrailingWhitespace(string a)
	{
		for (int i = a.Length - 1; i >= 0; i--)
		{
			if (a[i] != ' ' && a[i] != '\n' && a[i] != '\r' && a[i] != '\t' && a[i] != '\0')
			{
				return a.Substring(0, i + 1);
			}
		}
		return "";
	}
	//Returns true if a starts with b else false.
	public static bool StartsWith(string a, string b)
	{
		if (a is null && b is null)
		{
			return true;
		}
		else if (a is null || b is null)
		{
			return false;
		}
		return a.StartsWith(b);
	}
	//Returns true if a ends with b else false.
	public static bool EndsWith(string a, string b)
	{
		if (a is null && b is null)
		{
			return true;
		}
		else if (a is null || b is null)
		{
			return false;
		}
		return a.EndsWith(b);
	}
}