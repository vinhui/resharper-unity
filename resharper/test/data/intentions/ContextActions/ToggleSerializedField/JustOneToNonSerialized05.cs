using UnityEngine;
using JetBrains.Annotations;

public class Foo : MonoBehaviour
{
    [NotNull] public int myValue, my{caret:Make:field:'myValue2':non-serialized}Value2, myValue3;
}
