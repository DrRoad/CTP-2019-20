#include "lib/ArHosekSkyModel.h"
#include <iostream>

int main(int argc, char** argv)
{
	if (argc != 8) return -1;

	double solarElevation = atof(argv[1]);
	double turbidity = atof(argv[2]);
	double albedo[3] = { atof(argv[3]), atof(argv[4]), atof(argv[5]) };
	double theta = atof(argv[6]);
	double gamma = atof(argv[7]);

	ArHosekSkyModelState* skymodel_state[3];

	for (int i = 0; i < 3; i++) {
		skymodel_state[i] = arhosekskymodelstate_alloc_init(solarElevation, turbidity, albedo[i]);
	}

	float channel_center[3] = {
		700,    // Red 620740,
		546.1,  // Green 520570,
		435.8,  // Blue 450490,
	};
	
	double skydome_result[3];
	for (unsigned int i = 0; i < 3; i++) {
		skydome_result[i] = arhosekskymodel_radiance(skymodel_state[i], theta, gamma, channel_center[i]);
	}

	for (int i = 0; i < 3; i++) {
		arhosekskymodelstate_free(skymodel_state[i]);
	}

	std::cout << skydome_result[0] << std::endl << skydome_result[1] << std::endl << skydome_result[2] << std::endl;
	system("pause");

    return 0;
}

