using UnityEngine;
using System.Collections;

public interface ICurve {

    Vector2 startPoint();
    Vector2 endPoint();

    Vector2 pointOnCurve(float t);
    float parameterOnCurveArchLength(float normalizedLength, bool fromStart);
    Vector2 derivate1(float t, bool normalize);
    Vector2 derivate2(float t, bool normalize);
    
}
