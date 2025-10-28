#pragma once

#pragma pack(push, 4)

/**
 * @brief A runway identifier designator character.
 */
typedef enum FsRunwayIdentifierDesignator
{
	/**
	 * @brief The runway does not have a designator character.
	 */
	FsRunwayIdentifierDesignator_None,

	/**
	 * @brief A runway with a designator of L.
	 */
	FsRunwayIdentifierDesignator_Left,

	/**
	 * @brief A runway with a designator of R.
	 */
	FsRunwayIdentifierDesignator_Right,

	/**
	 * @brief A runway with a designator of C.
	 */
	FsRunwayIdentifierDesignator_Center,

	/**
	 * @brief A runway with a water type designator.
	 */
	FsRunwayIdentifierDesignator_Water,

	/**
	 * @brief A runway with a designator of A.
	 */
	FsRunwayIdentifierDesignator_A,

	/**
	 * @brief A runway with a designator of B.
	 */
	FsRunwayIdentifierDesignator_B
} FsRunwayIdentifierDesignator;

/**
 * @brief A runway identifier runway number.
 */
typedef enum FsRunwayIdentifierNumber
{
	/**
	 * @brief No runway is defined.
	 */
	FsRunwayIdentifierNumber_None,

	/**
	 * @brief A runway with the number 01.
	 */
	FsRunwayIdentifierNumber_1,

	/**
	 * @brief A runway with the number 02.
	 */
	FsRunwayIdentifierNumber_2,

	/**
	 * @brief A runway with the number 03.
	 */
	FsRunwayIdentifierNumber_3,

	/**
	 * @brief A runway with the number 04.
	 */
	FsRunwayIdentifierNumber_4,

	/**
	 * @brief A runway with the number 05.
	 */
	FsRunwayIdentifierNumber_5,

	/**
	 * @brief A runway with the number 06.
	 */
	FsRunwayIdentifierNumber_6,

	/**
	 * @brief A runway with the number 07.
	 */
	FsRunwayIdentifierNumber_7,

	/**
	 * @brief A runway with the number 08.
	 */
	FsRunwayIdentifierNumber_8,

	/**
	 * @brief A runway with the number 09.
	 */
	FsRunwayIdentifierNumber_9,

	/**
	 * @brief A runway with the number 10.
	 */
	FsRunwayIdentifierNumber_10,

	/**
	 * @brief A runway with the number 11.
	 */
	FsRunwayIdentifierNumber_11,

	/**
	 * @brief A runway with the number 12.
	 */
	FsRunwayIdentifierNumber_12,

	/**
	 * @brief A runway with the number 13.
	 */
	FsRunwayIdentifierNumber_13,

	/**
	 * @brief A runway with the number 14.
	 */
	FsRunwayIdentifierNumber_14,

	/**
	 * @brief A runway with the number 15.
	 */
	FsRunwayIdentifierNumber_15,

	/**
	 * @brief A runway with the number 16.
	 */
	FsRunwayIdentifierNumber_16,

	/**
	 * @brief A runway with the number 17.
	 */
	FsRunwayIdentifierNumber_17,

	/**
	 * @brief A runway with the number 18.
	 */
	FsRunwayIdentifierNumber_18,

	/**
	 * @brief A runway with the number 19.
	 */
	FsRunwayIdentifierNumber_19,

	/**
	 * @brief A runway with the number 20.
	 */
	FsRunwayIdentifierNumber_20,

	/**
	 * @brief A runway with the number 21.
	 */
	FsRunwayIdentifierNumber_21,

	/**
	 * @brief A runway with the number 22.
	 */
	FsRunwayIdentifierNumber_22,

	/**
	 * @brief A runway with the number 23.
	 */
	FsRunwayIdentifierNumber_23,

	/**
	 * @brief A runway with the number 24.
	 */
	FsRunwayIdentifierNumber_24,

	/**
	 * @brief A runway with the number 25.
	 */
	FsRunwayIdentifierNumber_25,

	/**
	 * @brief A runway with the number 26.
	 */
	FsRunwayIdentifierNumber_26,

	/**
	 * @brief A runway with the number 27.
	 */
	FsRunwayIdentifierNumber_27,

	/**
	 * @brief A runway with the number 28.
	 */
	FsRunwayIdentifierNumber_28,

	/**
	 * @brief A runway with the number 29.
	 */
	FsRunwayIdentifierNumber_29,

	/**
	 * @brief A runway with the number 30.
	 */
	FsRunwayIdentifierNumber_30,

	/**
	 * @brief A runway with the number 31.
	 */
	FsRunwayIdentifierNumber_31,

	/**
	 * @brief A runway with the number 32.
	 */
	FsRunwayIdentifierNumber_32,

	/**
	 * @brief A runway with the number 33.
	 */
	FsRunwayIdentifierNumber_33,

	/**
	 * @brief A runway with the number 34.
	 */
	FsRunwayIdentifierNumber_34,

	/**
	 * @brief A runway with the number 35.
	 */
	FsRunwayIdentifierNumber_35,

	/**
	 * @brief A runway with the number 36.
	 */
	FsRunwayIdentifierNumber_36,

	/**
	 * @brief A runway with the number N.
	 */
	FsRunwayIdentifierNumber_North,

	/**
	 * @brief A runway with the number NE.
	 */
	FsRunwayIdentifierNumber_Northeast,

	/**
	 * @brief A runway with the number E.
	 */
	FsRunwayIdentifierNumber_East,

	/**
	 * @brief A runway with the number SE.
	 */
	FsRunwayIdentifierNumber_Southeast,

	/**
	 * @brief A runway with the number S.
	 */
	FsRunwayIdentifierNumber_South,

	/**
	 * @brief A runway with the number SW.
	 */
	FsRunwayIdentifierNumber_Southwest,

	/**
	 * @brief A runway with the number W.
	 */
	FsRunwayIdentifierNumber_West,

	/**
	 * @brief A runway with the number NW.
	 */
	FsRunwayIdentifierNumber_Northwest
} FsRunwayIdentifierNumber;

/**
 * @brief The type of an approach procedure.
 */
typedef enum FsApproachProcedureType
{
	/**
	 * @brief No approach is defined.
	 */
	FsApproachProcedureType_None,

	/**
	 * @brief The approach procedure is a GPS type procedure.
	 */
	FsApproachProcedureType_Gps,

	/**
	 * @brief The approach procedure is a VOR type procedure.
	 */
	FsApproachProcedureType_Vor,

	/**
	 * @brief The approach procedure is a NDB type procedure.
	 */
	FsApproachProcedureType_Ndb,

	/**
	 * @brief The approach procedure is an ILS type procedure.
	 */
	FsApproachProcedureType_Ils,

	/**
	 * @brief The approach procedure is a LOC type procedure.
	 */
	FsApproachProcedureType_Localizer,

	/**
	 * @brief The approach procedure is a SDF type procedure.
	 */
	FsApproachProcedureType_Sdf,

	/**
	 * @brief The approach procedure is a LDA offset localizer type
	 * procedure.
	 */
	FsApproachProcedureType_Lda,

	/**
	 * @brief The approach procedure is a VOR DME type procedure.
	 */
	FsApproachProcedureType_VorDme,

	/**
	 * @brief The approach procedure is a NDB DME type procedure.
	 */
	FsApproachProcedureType_NdbDme,

	/**
	 * @brief The approach procedure is an RNAV or RNP type procedure.
	 */
	FsApproachProcedureType_Rnav,

	/**
	 * @brief The approach procedure is a LOC backcourse type procedure.
	 */
	FsApproachProcedureType_Localizer_Backcourse
} FsApproachProcedureType;

/**
 * @brief A facility ICAO identifier.
 */
typedef struct FsIcao
{
	/**
	 * @brief The single character that designates type of the facility
	 */
	char type = 0;

	/**
	 * @brief A two character null terminated string that designates
	 * the ICAO region where the facility resides.
	 */
	char region[3];

	/**
	 * @brief An eight character null terminated string that designates
	 * the related airport ident for the facility, if applicable.
	 */
	char airport[9];

	/**
	 * @brief An eight character null terminated string that designates
	 * the ident of the facility.
	 */
	char ident[9];
} FsIcao;

/**
 * @brief An identifier for an airport runway.
 */
typedef struct FsRunwayIdentifier
{
	/**
	 * @brief The runway number.
	 */
	FsRunwayIdentifierNumber number;

	/**
	 * @brief The runway designator suffix, if applicable (e.g. L, R, C, etc.).
	 */
	FsRunwayIdentifierDesignator designator;
} FsRunwayIdentifier;

/**
 * @brief A identifier for an approach procedure.
 */
typedef struct FsApproachIdentifier
{
	/**
	 * @brief The type of the approach procedure.
	 */
	FsApproachProcedureType type;

	/**
	 * @brief The runway this procedure is for, if applicable.
	 */
	FsRunwayIdentifier runway;

	/**
	 * @brief The single character null terminated string indicating
	 * the suffix of the approach (e.g. Z for RNAV 24 Z or A for VOR A).
	 */
	char suffix[2];
} FsApproachIdentifier;

#pragma pack(pop)
