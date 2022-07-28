using System;
using System.IO;
using System.Numerics;
using Vortice.Mathematics;
using UtilityLib;

namespace MeshLib;


//this is a cylinder shape with rounded ends
//Intended to work as a bone space capsule with the bottom
//sphere center at the joint, no translation and extend along Z
public struct BoundingCapsule
{
	internal float	mRadius;
	internal float	mLength;	//distance between cap sphere centers
								//not the actual full length of the capsule	

	internal BoundingCapsule(float rad, float len)
	{
		mRadius	=rad;
		mLength	=len;
	}


	internal BoundingCapsule(BinaryReader br)
	{
		mRadius	=br.ReadSingle();
		mLength	=br.ReadSingle();
	}


	internal void Write(BinaryWriter bw)
	{
		bw.Write(mRadius);
		bw.Write(mLength);
	}


	internal void Scale(float scalar)
	{
		mRadius	*=scalar;
		mLength	*=scalar;
	}


	static internal BoundingCapsule CreateFromBoundingBox(BoundingBox box)
	{
		//find radius
		float	radius	=0f;
		float	length	=0f;
		Vector3	radVec	=Vector3.Zero;

		//measure from center to most distant side
		if(box.Min.Z < box.Min.X && box.Min.Z < box.Min.Y)
		{
			//use Z for length
			length	=box.Max.Z - box.Min.Z;
			radVec	=Vector3.UnitX + Vector3.UnitY;	//xy for radius
		}
		else if(box.Min.Y < box.Min.X && box.Min.Y < box.Min.Z)
		{
			length	=box.Max.Y - box.Min.Y;
			radVec	=Vector3.UnitX + Vector3.UnitZ;	//xz for radius
		}
		else
		{
			length	=box.Max.X - box.Min.X;
			radVec	=Vector3.UnitZ + Vector3.UnitY;	//zy for radius
		}

		radVec	=Vector3.Normalize(radVec);
		radius	=Vector3.Dot(radVec, box.Max - box.Center);

		return	new BoundingCapsule(radius, length);
	}
	

	//from Jim Drygiannakis on github
	internal bool IntersectRay(Vector3 position, Vector3 orientation,	//capsule transform
		Ray ray,														//ray
		out Vector3 p1, out Vector3 p2,									//impact points
		out Vector3 n1, out Vector3 n2)									//impact normals
	{
		// http://pastebin.com/2XrrNcxb
		// Substituting equ. (1) - (6) to equ. (I) and solving for t' gives:
		//
		// t' = (t * dot(AB, d) + dot(AB, AO)) / dot(AB, AB); (7) or
		// t' = t * m + n where 
		// m = dot(AB, d) / dot(AB, AB) and 
		// n = dot(AB, AO) / dot(AB, AB)
		//

		//position is the bottom sphere center
		Vector3	A	=position;

		//find B which is the endpoint sphere center
		Vector3	B	=position + orientation * mLength;
		Vector3	AB	=B - A;
		Vector3	AO	=ray.Position - A;

		float	AB_dot_d	=Vector3.Dot(AB, ray.Direction);
		float	AB_dot_AO	=Vector3.Dot(AB, AO);
		float	AB_dot_AB	=Vector3.Dot(AB, AB);
		
		float	m	=AB_dot_d / AB_dot_AB;
		float	n	=AB_dot_AO / AB_dot_AB;

		// Substituting (7) into (II) and solving for t gives:
		//
		// dot(Q, Q)*t^2 + 2*dot(Q, R)*t + (dot(R, R) - r^2) = 0
		// where
		// Q = d - AB * m
		// R = AO - AB * n
		Vector3	Q	=ray.Direction - (AB * m);
		Vector3	R	=AO - (AB * n);

		float	a	=Vector3.Dot(Q, Q);
		float	b	=2.0f * Vector3.Dot(Q, R);
		float	c	=Vector3.Dot(R, R) - (mRadius * mRadius);

		if(a == 0f)
		{
			// Special case: AB and ray direction are parallel. If there is an intersection it will be on the end spheres...
			// NOTE: Why is that?
			// Q = d - AB * m =>
			// Q = d - AB * (|AB|*|d|*cos(AB,d) / |AB|^2) => |d| == 1.0
			// Q = d - AB * (|AB|*cos(AB,d)/|AB|^2) =>
			// Q = d - AB * cos(AB, d) / |AB| =>
			// Q = d - unit(AB) * cos(AB, d)
			//
			// |Q| == 0 means Q = (0, 0, 0) or d = unit(AB) * cos(AB,d)
			// both d and unit(AB) are unit vectors, so cos(AB, d) = 1 => AB and d are parallel.
			// 
			BoundingSphere	sphereA	=new BoundingSphere(A, mRadius);
			BoundingSphere	sphereB	=new BoundingSphere(B, mRadius);

			float	atmin, atmax, btmin, btmax;
			
			if(!Mathery.IntersectRaySphere(ray, sphereA, out atmin, out atmax) ||
				!Mathery.IntersectRaySphere(ray, sphereB, out btmin, out btmax))
			{
				// No intersection with one of the spheres means no intersection at all...
				p1	=p2	=n1	=n2	=Vector3.Zero;
				return	false;
			}

			if(atmin < btmin)
			{
				p1	=ray.Position + (ray.Direction * atmin);
				n1	=p1 - A;
				n1	=Vector3.Normalize(n1);
			}
			else
			{
				p1	=ray.Position + (ray.Direction * btmin);
				n1	=p1 - B;
				n1	=Vector3.Normalize(n1);
			}

			if(atmax > btmax)
			{
				p2	=ray.Position + (ray.Direction * atmax);
				n2	=p2 - A;
				n2	=Vector3.Normalize(n2);
			}
			else
			{
				p2	=ray.Position + (ray.Direction * btmax);
				n2	=p2 - B;
				n2	=Vector3.Normalize(n2);
			}

			return true;
		}

		float	discriminant	=b * b - 4.0f * a * c;
		if(discriminant < 0.0f)
		{
			// The ray doesn't hit the infinite cylinder defined by (A, B).
			// No intersection.
			p1	=p2	=n1	=n2	=Vector3.Zero;
			return	false;
		}

		float	tmin	=(-b - MathHelper.Sqrt(discriminant)) / (2.0f * a);
		float	tmax	=(-b + MathHelper.Sqrt(discriminant)) / (2.0f * a);
		if(tmin > tmax)
		{
			float	temp	=tmin;

			tmin	=tmax;
			tmax	=temp;
		}

		// Now check to see if K1 and K2 are inside the line segment defined by A,B
		float	t_k1	=tmin * m + n;
		if(t_k1 < 0f)
		{
			// On sphere (A, r)...
			BoundingSphere	s	=new BoundingSphere(A, mRadius);

			float	stmin, stmax;
			if(Mathery.IntersectRaySphere(ray, s, out stmin, out stmax))
			{
				p1	=ray.Position + (ray.Direction * stmin);
				n1	=p1 - A;
				n1	=Vector3.Normalize(n1);
			}
			else
			{
				p1	=p2	=n1	=n2	=Vector3.Zero;
				return	false;
			}
		}
		else if(t_k1 > 1f)
		{
			// On sphere (B, r)...
			BoundingSphere	s	=new BoundingSphere(B, mRadius);

			float	stmin, stmax;
			if(Mathery.IntersectRaySphere(ray, s, out stmin, out stmax))
			{
				p1	=ray.Position + (ray.Direction * stmin);
				n1	=p1 - B;
				n1	=Vector3.Normalize(n1);
			}
			else
			{
				p1	=p2	=n1	=n2	=Vector3.Zero;
				return	false;
			}
		}
		else
		{
			// On the cylinder...
			p1	=ray.Position + (ray.Direction * tmin);

			Vector3	k1	=A + AB * t_k1;
			n1	=p1 - k1;
			n1	=Vector3.Normalize(n1);
		}

		float	t_k2	=tmax * m + n;
		if(t_k2 < 0f)
		{
			// On sphere (A, r)...
			BoundingSphere	s	=new BoundingSphere(A, mRadius);

			float	stmin, stmax;
			if(Mathery.IntersectRaySphere(ray, s, out stmin, out stmax))
			{
				p2	=ray.Position + (ray.Direction * stmax);
				n2	=p2 - A;
				n2	=Vector3.Normalize(n2);
			}
			else
			{
				p1	=p2	=n1	=n2	=Vector3.Zero;
				return	false;
			}
		}
		else if(t_k2 > 1f)
		{
			// On sphere (B, r)...
			BoundingSphere	s	=new BoundingSphere(B, mRadius);

			float	stmin, stmax;
			if(Mathery.IntersectRaySphere(ray, s, out stmin, out stmax))
			{
				p2	=ray.Position + (ray.Direction * stmax);
				n2	=p2 - B;
				n2	=Vector3.Normalize(n2);
			}
			else
			{
				p1	=p2	=n1	=n2	=Vector3.Zero;
				return	false;
			}
		}
		else
		{
			p2	=ray.Position + (ray.Direction * tmax);

			Vector3	k2	=A + AB * t_k2;
			n2	=p2 - k2;
			n2	=Vector3.Normalize(n2);
		}
		
		return	true;
	}


	// Ray and cylinder intersection
	// If hit, returns true and the intersection point in 'newPosition' with a normal and distance along
	// the ray ('lambda')
	bool d3RayCylinderIntersection(Vector3 cylPos, Vector3 orientation,
		Vector3 org, Vector3 dir,
		out float lambda, out Vector3 normal, out Vector3 newPosition)
	{
		lambda		=0;
		normal		=Vector3.Zero;
		newPosition	=Vector3.Zero;

		Vector3	RC;
		float	d;
		float	t, s;
		Vector3	n, D, O;
		float	ln;
		float	inn, outt;
		
		RC	=org - cylPos;

		n	=Vector3.Cross(dir, orientation);
		
		ln	=n.Length();
		
		//Parallel? (?)
		if(Mathery.CompareFloatEpsilon(ln, 0f, Mathery.ANGLE_EPSILON))
		{
			return false;
		}

		//normalize
		n	/=ln;
		
		d	=Math.Abs(Vector3.Dot(RC, n));
		
		if(d <= mRadius)
		{
			O	=Vector3.Cross(RC, orientation);
			t	=-Vector3.Dot(O, n) / ln;

			O	=Vector3.Cross(n, orientation);
			O	=Vector3.Normalize(O);

			s	=Math.Abs(
					MathHelper.Sqrt(mRadius * mRadius - d * d) /
					Vector3.Dot(dir, O));

			inn		=t - s;
			outt	=t + s;

			if(inn < -Mathery.ANGLE_EPSILON)
			{
				if(outt < -Mathery.ANGLE_EPSILON)
				{
					return	false;
				}
				else
				{
					lambda	=outt;
				}
			}
			else if(inn < outt)
			{
				lambda	=inn;
			}
			else
			{
				lambda	=outt;
			}
			
			//Calculate intersection point
			newPosition	=org + dir * lambda;
			
			Vector3	HB	=newPosition - cylPos;
			
			float	scale	=Vector3.Dot(HB, orientation);

			normal	=HB - orientation * scale;

			normal	=Vector3.Normalize(normal);
			
			return true;
		}
		return false;
	}


	//returned impact should be along the ray
	internal bool RayCollide(Vector3 position, Vector3 orientation,
		Vector3	rayStart, Vector3 rayEnd, float rayRadius, out Vector3 impacto)
	{
		float	bothRadii	=(mRadius + rayRadius);
		Vector3	rayToCenter	=rayStart - position;
		Vector3	ray			=rayEnd - rayStart;

		//see if ray is aimed away
		float	dir	=Vector3.Dot(-rayToCenter, ray);
		if(dir < 0f)
		{
			impacto	=Vector3.Zero;
			return	false;
		}

		//position is the bottom sphere center
		Vector3	A	=position;

		//find B which is the endpoint sphere center
		Vector3	B	=position + orientation * mLength;

		//rayStart is C, the start point of the ray
		//D is the end point of the ray
		Vector3	D	=rayEnd;

		Vector3	segStart, segEnd;
		bool	bSolved	=Mathery.ShortestLineBetweenTwoLines(A, B, rayStart, D,
			out segStart, out segEnd);

		if(!bSolved)
		{
			//happens with bad data I think
			impacto	=Vector3.Zero;
			return	false;
		}

		Vector3	segVector	=segEnd - segStart;

		float	dist	=segVector.Length();
		if(dist > bothRadii)
		{
			//no hit
			impacto	=Vector3.Zero;
			return	false;
		}

		if(dist == 0f)
		{
			//right atop
			impacto	=segStart;
			return	true;
		}

		//A line segment has been found that is shorter than
		//the combined radiusesesesees of the capsule and the line...
		//
		//Now check if the segment lies between the two endpoints of the capsule.
		//this is checking the cylinder portion of the capsule

		//plane at the base of the capsule...
		Vector3	baseNormal	=orientation;
		float	baseDist	=Vector3.Dot(baseNormal, A);

		//plane at the end of the capsule
		Vector3	endNormal	=-orientation;
		float	endDist		=Vector3.Dot(endNormal, B);

		//segstart / end will always have the same distance to these planes
		float	segBaseDist	=Vector3.Dot(baseNormal, segStart) - baseDist;
		float	segEndDist	=Vector3.Dot(endNormal, segStart) - endDist;

		if(segBaseDist >= 0 && segEndDist >= 0)
		{
			//segment is within the cylinder
			//find impact

			//segStart is along the capsule line between A and B
			//segEnd is on the ray
			//
			//impacto should be the point along the ray where the
			//distance to capsule line AB is mRadius + rayRadius
			//and make sure impacto is still within the ray segment
			//
			//using a solution I found off stacko from Ruud van Gaal
			Vector3	raySegment	=segEnd - rayStart;
			Vector3	rayVec		=Vector3.Normalize(raySegment);

			Vector3	n		=Vector3.Cross(rayVec, orientation);
			float	nLen	=n.Length();

			//normalize
			n	/=nLen;

			float	d	=Math.Abs(Vector3.Dot(rayToCenter, n));

			Vector3	raySide	=Vector3.Cross(rayToCenter, orientation);			

			float	t	=-Vector3.Dot(raySide, n) / nLen;

			raySide	=Vector3.Cross(n, orientation);
			raySide	=Vector3.Normalize(raySide);

			float	s	=Math.Abs(
					MathHelper.Sqrt(bothRadii * bothRadii - d * d) /
					Vector3.Dot(rayVec, raySide));

			float	frontHit	=t - s;
			float	backHit		=t + s;
			float	rayHitRatio;

			if(frontHit < -Mathery.ANGLE_EPSILON)
			{
				if(backHit < -Mathery.ANGLE_EPSILON)
				{
					impacto	=segStart;
					return	false;
				}
				else
				{
					rayHitRatio	=backHit;
				}
			}
			else if(frontHit < backHit)
			{
				rayHitRatio	=frontHit;
			}
			else
			{
				rayHitRatio	=backHit;
			}

			impacto	=rayStart + rayVec * rayHitRatio;

			//testing dist == radius
			/*
			float	testDist;
			if(Mathery.PointLineSegDistance(impacto, A, B, out testDist))
			{
				int	gack	=69;
				gack++;
			}
			else
			{
				int	gack	=69;
				gack++;
			}*/

			return	true;
		}

		//Shortest line segment is not within the cylinder section...
		//Check the end spheres

		//check A sphere
		BoundingSphere	bs	=new BoundingSphere(A, bothRadii);

		float	?hit	=Mathery.RayIntersectSphere(rayStart, rayEnd, bs);
		if(hit != null)
		{
			ray		=Vector3.Normalize(ray);
			impacto	=rayStart + (ray * hit.Value);
			return	true;
		}

		//check B sphere
		bs	=new BoundingSphere(B, bothRadii);
		hit	=Mathery.RayIntersectSphere(rayStart, rayEnd, bs);
		if(hit != null)
		{
			ray		=Vector3.Normalize(ray);
			impacto	=rayStart + (ray * hit.Value);
			return	true;
		}

		impacto	=Vector3.Zero;
		return	false;
	}



}
