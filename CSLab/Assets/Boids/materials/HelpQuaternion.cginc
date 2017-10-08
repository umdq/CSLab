
//hlsl没提供关于quaternion的内置函数，只好from scratch
float4 newQuat(float3 startVec,float3 endVec)
{
	float halfAngle=acos(dot(startVec,endVec))/2.0f;
	//unity坐标数据使用的是左手坐标系，不过这里不需要care这点，因为有cross自动调，只需按始末顺序
	float3 rotationAxis=normalize(cross(startVec,endVec));
	return float4(sin(halfAngle)*rotationAxis,cos(halfAngle));
}

float4 multQuat(float4 q1, float4 q2)
{
	return float4(
	q1.w * q2.x + q1.x * q2.w + q1.z * q2.y - q1.y * q2.z,
	q1.w * q2.y + q1.y * q2.w + q1.x * q2.z - q1.z * q2.x,
	q1.w * q2.z + q1.z * q2.w + q1.y * q2.x - q1.x * q2.y,
	q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z
	);
}

float3 rotateVec( float4 quat, float3 vec )
{
	float4 qv = multQuat( quat, float4(vec, 0.0) );
	return multQuat( qv, float4(-quat.x, -quat.y, -quat.z, quat.w) ).xyz;
}