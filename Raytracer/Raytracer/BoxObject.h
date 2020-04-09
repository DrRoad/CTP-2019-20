#pragma once

#include "Object.h"

class BoxObject : public Object
{
public:
	BoxObject(const Vec3f &b0, const Vec3f &b1, const Vec3f _colour) : Object(_colour) {
		bounds[0] = b0;
		bounds[1] = b1;
		center = (b1 - b0) + b0;
	}

	bool intersect(const Vec3f &orig, const Vec3f &dir, float &t);
	void getSurfaceData(const Vec3f &Phit, Vec3f &Nhit, Vec2f &tex);

private:
	Vec3f bounds[2];
	Vec3f center;
};