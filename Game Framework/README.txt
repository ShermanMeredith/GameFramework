# PlayTable Game Framework

## Naming Convention
const		TYPE_USAGE_OPTIONAL		internal const string URL_REGISTER = "http://xx.xx.xx.xx:8080/account/registration/";
readonly	type_usage_optional		public static readonly Color purple_dark = new Color(73f / 255f, 23f / 255f, 91f / 255f, 1f);
enum		UpperCamelCase	    		public enum PTDirection { Unknown=-1, Up, Right, Down,Left, RightUp, RightDown, LeftUp, LeftDown };
class 		UpperCamelCase	    
Method		UpperCamelCase	 
Property	UpperCamelCase			public int countCard {get{return transform.childCount;}}
Interface 	IUpperCamelCase
Generic Type	T				public abstract Dictionary<TypeA, TypeB> MyMethod<TypeA, TypeB>(TypeA inputA, TypeB inputB); 
private		_lowerCamelCase
public		lowerCamelCase
protected	lowerCamelCase
internal	lowerCamelCase
local		lowerCamelCase		
parameter 	lowerCamelCase		
default		lowerCamelCase		
(Never use one letter in any cases other than Generic Type)

## Project Structure



