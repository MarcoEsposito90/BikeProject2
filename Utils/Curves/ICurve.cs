using UnityEngine;
using System.Collections;

public interface ICurve {

    Vector2 startPoint();
    Vector2 endPoint();
    float length();

    Vector2 pointOnCurve(float t);

    float parameterOnCurveArchLength(float normalizedLength, bool fromStart);
    Vector2 getRightVector(float t);
    Vector2 derivate1(float t, bool normalize);
    Vector2 derivate2(float t, bool normalize);
    
}
