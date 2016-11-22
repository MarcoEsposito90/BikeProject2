using UnityEngine;
using System.Collections;
using System.Reflection;

public interface IMeshModifier {

    void Apply(MeshData meshData);
}
