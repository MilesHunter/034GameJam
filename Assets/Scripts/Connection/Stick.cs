using UnityEngine;
using System.Collections.Generic;

public class Stick : MonoBehaviour {
    public Rigidbody2D rb;
    [SerializeField] float attractionForce = 10f;
    [SerializeField] float connectionThreshold = 0.1f;
    [SerializeField] Transform endA, endB;
    public Ball endpointA, endpointB;
    private List<FixedJoint2D> connectedJoints = new List<FixedJoint2D>();

    Vector2 unattachedEnd {
        get {
            if (endpointA == null) return endA.position;
            if (endpointB == null) return endB.position;
            return Vector2.zero;
        }
    }

    void FixedUpdate() {
        if(!endpointA) AttractBalls(endpointB);
        if(!endpointB) AttractBalls(endpointA);
    }

    void AttractBalls(Ball connectedBall) {
        Collider2D[] balls = Physics2D.OverlapCircleAll(
            unattachedEnd, 
            2f, 
            LayerMask.GetMask("Ball")
        );
        
        foreach(var ballColl in balls) {
            Ball ball = ballColl.GetComponent<Ball>();
            if(ball && ball.CanConnect()) {
                Vector2 dir = (unattachedEnd - (Vector2)ball.transform.position).normalized;
                ball.GetComponent<Rigidbody2D>().AddForce(dir * attractionForce);
                
                if(Vector2.Distance(unattachedEnd, ball.transform.position) < connectionThreshold) {
                    EstablishConnection(ball);
                }
            }
        }
    }

    void EstablishConnection(Ball ball) {
        if (endpointA == null) {
            endpointA = ball;
        } else if (endpointB == null) {
            endpointB = ball;
        }
        
        ball.AddConnection(this);
        
        // 创建物理连接
        FixedJoint2D joint = gameObject.AddComponent<FixedJoint2D>();
        joint.connectedBody = ball.GetComponent<Rigidbody2D>();
        connectedJoints.Add(joint);
    }
}
