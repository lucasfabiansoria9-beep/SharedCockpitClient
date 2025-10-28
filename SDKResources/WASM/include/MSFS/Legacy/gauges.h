// gauges.h
// Copyright (c) 2003 Microsoft Corporation.  All rights reserved.
//

#ifndef GAUGE_H__
#define GAUGE_H__

#include "..\MSFS_Core.h"
#include "..\MSFS_WindowsTypes.h"
#include "..\MSFS_Render.h"
#include "..\Types\MSFS_EventsEnum.h"
#include "..\MSFS_GaugeContext.h"

#pragma pack(push,4)

// Useful macros for INL files
#define CONCATENATE1(A,B) A##B
#define CONCATENATE(A,B) CONCATENATE1(A,B)
#define CONCATENATE31(A,B,C) A##B##C
#define CONCATENATE3(A,B,C) CONCATENATE31(A,B,C)
#define C_GAUGE(A) CONCATENATE(A , GAUGE_ITEM)
#define C_PLIST(A) CONCATENATE(plist_ , A)

#define LENGTHOF(array) (sizeof(array)/sizeof(array[0]))

#ifndef EXTERN_C
#ifdef __cplusplus
#define EXTERN_C    extern "C"
#else
#define EXTERN_C    extern
#endif
#endif


#ifndef DECLSPEC_EXPORT
#define DECLSPEC_EXPORT __declspec(dllexport)
#endif

// Flightsim calling convention
#ifdef _MSFS_WASM
#define FSAPI
#else
#define FSAPI   __stdcall
#endif

//
// Common Flight Simulator Constant Definitions
//

// parameter direction tags
#ifndef IN
#define IN
#endif

#ifndef OUT
#define OUT
#endif

#ifndef VOID
#define VOID    void
#endif

// Logical True/False Constants
#ifndef FALSE
#define FALSE   0
#endif

#ifndef TRUE
#define TRUE    1
#endif

// Pointer Constant
#ifndef NULL
#define NULL    ((void *)0)         // empty handle
#endif

// Misc Constants
#define NULLPTR NULL                // empty pointer
#define CR      0x0d                // ASCII carriage return
#define LF      0x0a                // ASCII line feed
#define FLAGS0  0                   // no flags set in a FLAGSn type variable
#define FLAGS1  -1                  // all flags set in a FLAGSn type variable

// Bit Flag Constants
#define BIT0    0x00000001
#define BIT1    0x00000002
#define BIT2    0x00000004
#define BIT3    0x00000008
#define BIT4    0x00000010
#define BIT5    0x00000020
#define BIT6    0x00000040
#define BIT7    0x00000080
#define BIT8    0x00000100
#define BIT9    0x00000200
#define BIT10   0x00000400
#define BIT11   0x00000800
#define BIT12   0x00001000
#define BIT13   0x00002000
#define BIT14   0x00004000
#define BIT15   0x00008000
#define BIT16   0x00010000
#define BIT17   0x00020000
#define BIT18   0x00040000
#define BIT19   0x00080000
#define BIT20   0x00100000
#define BIT21   0x00200000
#define BIT22   0x00400000
#define BIT23   0x00800000
#define BIT24   0x01000000
#define BIT25   0x02000000
#define BIT26   0x04000000
#define BIT27   0x08000000
#define BIT28   0x10000000
#define BIT29   0x20000000
#define BIT30   0x40000000
#define BIT31   0x80000000

// integer data types limits
#define UINT8_MIN   0
#ifndef _MSFS_WASM
#define UINT8_MAX   255
#endif
#define SINT8_MIN   -128
#define SINT8_MAX    127
#define UINT16_MIN  0
#ifndef _MSFS_WASM
#define UINT16_MAX  65535
#endif
#define SINT16_MIN  -32768
#define SINT16_MAX  32767
#define UINT32_MIN  0
#ifndef _MSFS_WASM
#define UINT32_MAX  4294967295
#endif
#define SINT32_MIN  ((signed int) BIT31)
#define SINT32_MAX  2147483647

// fractional data types limits
#define UFRAC8_MIN  0
#define UFRAC8_MAX  255
#define SFRAC8_MIN  -128
#define SFRAC8_MAX  127
#define UFRAC16_MIN 0
#define UFRAC16_MAX 65535
#define SFRAC16_MIN -32768
#define SFRAC16_MAX 32767
#define UFRAC32_MIN 0
#define UFRAC32_MAX 4294967295
#define SFRAC32_MIN -2147483648
#define SFRAC32_MAX 2147483647

// flag values for engine_control_select variable
#define ENGINE_CONTROL_SELECT_NONE  0       // no engines
#define ENGINE_CONTROL_SELECT_1     BIT0    // engine 1
#define ENGINE_CONTROL_SELECT_2     BIT1    // engine 2
#define ENGINE_CONTROL_SELECT_3     BIT2    // engine 3
#define ENGINE_CONTROL_SELECT_4     BIT3    // engine 4

// realism_systems values
#define REALISM_SYSTEMS_ELEV_TRIM   BIT1    // enable elevator trim realism (ratchet effect)
#define REALISM_SYSTEMS_GYRO_DRIFT  BIT2    // enable gyro drift
#define REALISM_SYSTEMS_LIGHT_BURN  BIT3    // enable light burnout if on too long during day
#define REALISM_SYSTEMS_FAST_THROT  BIT4    // enable fast throttle kill engine
#define REALISM_SYSTEMS_INS_LIGHTS  BIT5    // enable instrument light needed at night to see panel
#define REALISM_SYSTEMS_BARO_DRIFT  BIT6    // enable barometric pressure drift


// defines for window layer numbers & shared buffer numbers
#define LOWEST_LAYER        0x0000
#define VIEW_LAYER          0x2000
#define MINICONTROL_LAYER   0x3000
#define AUX_VIEW_LAYER      0x3F00
#define PANEL_LAYER         0x4000
#define MESSAGE_LAYER       0x5000
#define MENU_LAYER          0x6000
#define HIGHEST_LAYER       0xffff


// constants for the range of the UNIPIX coordinate system
#define UNIPIX_MAX_HEIGHT   6144    // max height of unipix region
#define UNIPIX_MAX_WIDTH    8192    // max width of unipix region


// vor1_tf_flag and vor2_tf_flag
#define VOR_TF_FLAG_OFF         0
#define VOR_TF_FLAG_TO          1
#define VOR_TF_FLAG_FROM        2

// Back course flags for VOR gauges
#define BC_FLAG_BACKCOURSE_AVAILABLE    BIT0
#define BC_FLAG_LOCALIZER_TUNED_IN      BIT1
#define BC_FLAG_ON_BACKCOURSE           BIT2
#define BC_FLAG_STATION_ACTIVE          BIT7

// sim_speed constants
#define SIM_SPEED_QUARTER   0x0040
#define SIM_SPEED_HALF      0x0080
#define SIM_SPEED_1X        0x0100
#define SIM_SPEED_2X        0x0200
#define SIM_SPEED_4X        0x0400
#define SIM_SPEED_8X        0x0800
#define SIM_SPEED_16X       0x1000
#define SIM_SPEED_32X       0x2000
#define SIM_SPEED_64X       0x4000
#define SIM_SPEED_128X      0x8000

#define SIM_SPEED_MIN       SIM_SPEED_QUARTER
#define SIM_SPEED_MAX       SIM_SPEED_128X

// controls limits
#define SPOILERS_POS_OFF            0       // spoilers desired position {0=retracted..16k=fully extended}
#define SPOILERS_POS_FULL           16383
#define FLAPS_POS_OFF               0       // 0=up, 16383 = full
#define FLAPS_POS_FULL              16383
#define GEAR_POS_UP                 0       // 0 = up, 16K = dn
#define GEAR_POS_DOWN               16383

// defines for VOR_INFO.CODE field
#define VOR_CODE_IS_LOCALIZER           BIT7    // bit7 = 0= VOR  1= Localizer
#define VOR_CODE_GLIDESLOPE             BIT6    // bit6 = 1= Glideslope Available
#define VOR_CODE_BACKCOURSE_UNAVAIL     BIT5    // bit5 = 1= no localizer backcourse
#define VOR_CODE_DME_AT_GLIDE_SLOPE     BIT4    // bit4 = 1= DME transmitter at Glide Slope Transmitter
#define VOR_CODE_NAV_UNAVAILABLE        BIT3    // bit3 = 1= no nav signal available
#define VOR_CODE_VOICE_AVAILABLE        BIT2    // bit2 = Voice Available
#define VOR_CODE_TACAN                  BIT1    // bit1 = TACAN
#define VOR_CODE_DME_AVAILABLE          BIT0    // bit0 = DME

// defines for engine structure starter variable, note:  magnetos for recip. engines / starters for jet engines
#define ENG_STARTER_MAGNETO_OFF     0   // the following values for reciprocating engines
#define ENG_STARTER_MAGNETO_RIGHT   1
#define ENG_STARTER_MAGNETO_LEFT    2
#define ENG_STARTER_MAGNETO_BOTH    3
#define ENG_STARTER_MAGNETO_START   4
#define ENG_STARTER_STARTER_OFF     0   // the following values for jet engines
#define ENG_STARTER_STARTER_START   1
#define ENG_STARTER_STARTER_GEN     2


#define GST_UNUSED  0x0000
#define GST_ACTIVE  0x0001
#define GST_GEAR    0x0002
#define GST_SCRAPE  0x0004

#define GSN_CENTER_GEAR         0
#define GSN_LEFT_GEAR           1
#define GSN_RIGHT_GEAR          2
#define GSN_AUX_GEAR            3
#define GSN_RIGHT_WING_SCRAPE   4
#define GSN_LEFT_WING_SCRAPE    5
#define GSN_FUSELAGE_SCRAPE     6
#define GSN_XTAIL_SCRAPE        7
#define GSN_AUX1_SCRAPE         8
#define GSN_AUX2_SCRAPE         9

#define TANK_MAIN_RIGHT     0
#define TANK_MAIN_LEFT      1
#define TANK_AUX_RIGHT      2
#define TANK_AUX_LEFT       3
#define TANK_TIP_RIGHT      4
#define TANK_TIP_LEFT       5
#define TANK_CENTER         6
#define TANK_CENTER2        7
#define TANK_CENTER3        8
#define TANK_EXTERNAL1      9
#define TANK_EXTERNAL2      10

// gear types
#define GEAR_TYPE_FIXED         0
#define GEAR_TYPE_RETRACTABLE   1
#define GEAR_TYPE_SKIDS         2
#define GEAR_TYPE_FLOATS        3
#define GEAR_TYPE_SKIS          4


// time_of_day constants
#define TIME_OF_DAY_DAY     BIT0
#define TIME_OF_DAY_DAWN    BIT1
#define TIME_OF_DAY_DUSK    BIT1
#define TIME_OF_DAY_NIGHT   BIT2

// surface_type constants
typedef enum SURFACE_TYPE
{
	SURFACE_TYPE_CONCRETE = 0,    // concrete
	SURFACE_TYPE_GRASS = 1,    // soft, bumpy ground (landable)
	SURFACE_TYPE_WATER = 2,    // water
	SURFACE_TYPE_GRASS_BUMPY = 3,    // very bumpy grass & mud (crashable)
	SURFACE_TYPE_ASPHALT = 4,    // asphalt
	SURFACE_TYPE_SHORT_GRASS = 5,    // short grass
	SURFACE_TYPE_LONG_GRASS = 6,    // long grass
	SURFACE_TYPE_HARD_TURF = 7,    // hard turf
	SURFACE_TYPE_SNOW = 8,
	SURFACE_TYPE_ICE = 9,
	SURFACE_TYPE_URBAN = 10,
	SURFACE_TYPE_FOREST = 11,
	SURFACE_TYPE_DIRT = 12,   // added for dirt runways
	SURFACE_TYPE_CORAL = 13,   // added for coral runways
	SURFACE_TYPE_GRAVEL = 14,   // added for gravel runways
	SURFACE_TYPE_OIL_TREATED = 15,   // added for oil treated (tar&chip) runways
	SURFACE_TYPE_STEEL_MATS = 16,   // added for steel mats (steel mesh) temporary runways
	SURFACE_TYPE_BITUMINUS = 17,
	SURFACE_TYPE_BRICK = 18,
	SURFACE_TYPE_MACADAM = 19,
	SURFACE_TYPE_PLANKS = 20,
	SURFACE_TYPE_SAND = 21,
	SURFACE_TYPE_SHALE = 22,
	SURFACE_TYPE_TARMAC = 23,
	SURFACE_TYPE_WRIGHT_FLYER_TRACK = 24,

	SURFACE_TYPE_UNKNOWN = 254,  // valid but unknown surface type
	SURFACE_TYPE_UNDEFINED = 255   // invalid surface type
}
SURFACE_TYPE, *PSURFACE_TYPE;

typedef enum SURFACE_CONDITION
{
	SURFACE_CONDITION_NORMAL = 0,    // default for the given SURFACE_TYPE
	SURFACE_CONDITION_WET,
	SURFACE_CONDITION_ICY,
	SURFACE_CONDITION_SNOW,             // this is for snow on a non-snow SURFACE_TYPE
	SURFACE_CONDITION_MAX       // this surface condition should always be last
}
SURFACE_CONDITION;


#define FS_REG_BASE     "SOFTWARE\\Microsoft\\Microsoft Games\\Flight Simulator\\10.0"

enum TOOLTIP_UNITS_TYPE {
	TOOLTIP_UNITS_TYPE_DEFAULT,
	TOOLTIP_UNITS_TYPE_METRIC,
	TOOLTIP_UNITS_TYPE_US,
};


// Panel quick help strings
#define HELPID_MIN                              0x0000a000

#define HELPID_CESS                             (HELPID_MIN + 0x0000)
#define HELPID_CESS_CARB_HEAT                   (HELPID_CESS + 0x000f)
#define HELPID_CESS_FUEL_PUMP_TOGGLE            (HELPID_CESS + 0x003c)
#define HELPID_CESS_PRIMER_SWITCH               (HELPID_CESS + 0x003D)
#define HELPID_LEAR                             (HELPID_MIN + 0x0100)
#define HELPID_LEAR_AOA                         (HELPID_LEAR + 0x0004)
#define HELPID_LEAR_COM_RADIO                   (HELPID_LEAR + 0x0027)
#define HELPID_LEAR_NAV1                        (HELPID_LEAR + 0x0028)
#define HELPID_LEAR_NAV2                        (HELPID_LEAR + 0x0029)
#define HELPID_LEAR_MACH                        (HELPID_LEAR + 0x0038)
#define HELPID_LEAR_VOR2_NEEDLE_TOGGLE          (HELPID_LEAR + 0x003d)
#define HELPID_LEAR_ADF_NEEDLE_TOGGLE           (HELPID_LEAR + 0x00de)
#define HELPID_LEAR_MACH_INDICATOR              (HELPID_LEAR + 0x00e1)
#define HELPID_LEAR_FUEL_STANDBY                (HELPID_LEAR + 0x00E5)
#define HELPID_SOPWITH                          (HELPID_MIN + 0x0200)
#define HELPID_SOPWITH_OILP                     (HELPID_SOPWITH + 0x0005)
#define HELPID_SOPWITH_MAG                      (HELPID_SOPWITH + 0x0006)
#define HELPID_SCHWEIZER                        (HELPID_MIN + 0x0300)
#define HELPID_SCHWEIZER_ASI                    (HELPID_SCHWEIZER + 0x0002)
#define HELPID_SCHWEIZER_VSI                    (HELPID_SCHWEIZER + 0x0006)
#define HELPID_B737                             (HELPID_MIN + 0x0400)
#define HELPID_B737_OIL_QUANT                   (HELPID_B737 + 0x001a)
#define HELPID_B737_HYDRAULIC                   (HELPID_B737 + 0x001b)
#define HELPID_B737_TOTAL_AIR_TEMP              (HELPID_B737 + 0x0031)
#define HELPID_B737_VIBRATION                   (HELPID_B737 + 0x0033)
#define HELPID_B737_HYDRAULIC_QTY               (HELPID_B737 + 0x0034)
#define HELPID_B737_AUTOPILOT_IAS_MACH_SELECT   (HELPID_B737 + 0x0037)
#define HELPID_B737_TO_GA                       (HELPID_B737 + 0x0038)
#define HELPID_B737_AUTOTHROTTLE                (HELPID_B737 + 0x0039)
#define HELPID_B737_REFERENCE_N1                (HELPID_B737 + 0x003a)
#define HELPID_B737_N1_SET                      (HELPID_B737 + 0x003b)
#define HELPID_B737_TAKEOFF_POWER_ACTIVE        (HELPID_B737 + 0x0044)
#define HELPID_B737_AT_LIM                      (HELPID_B737 + 0x0046)
#define HELPID_B737_FLAPS_EXT_LIGHT             (HELPID_B737 + 0x0048)
#define HELPID_B737_FLAPS_TRANSIT_LIGHT         (HELPID_B737 + 0x0049)
#define HELPID_B737_OIL_PRESSURE_LIGHT          (HELPID_B737 + 0x004A)
#define HELPID_B737_REVERSER_UNLOCKED           (HELPID_B737 + 0x004B)
#define HELPID_B737_START_VALVE_LIGHT           (HELPID_B737 + 0x004C)
#define HELPID_B737_START_LEVERS                (HELPID_B737 + 0x0051)
#define HELPID_EXTRA                            (HELPID_MIN + 0x0500)
#define HELPID_EXTR_SMOKE                       (HELPID_EXTRA + 0x0013)
#define HELPID_EXTR_ACRO_TANK                   (HELPID_EXTRA + 0x0014)
#define HELPID_EXTR_MAIN_TANK                   (HELPID_EXTRA + 0x0015)
#define HELPID_EXTR_G_METER                     (HELPID_EXTRA + 0x001c)
#define HELPID_EXTR_G_METER_RESET               (HELPID_EXTRA + 0x0027)
#define HELPID_EXTR_PEDAL_SWITCH_LEFT           (HELPID_EXTRA + 0x002D)
#define HELPID_EXTR_PEDAL_SWITCH_RIGHT          (HELPID_EXTRA + 0x002E)
#define HELPID_EXTR_LOW_VOLT                    (HELPID_EXTRA + 0x002F)
#define HELPID_BELL                             (HELPID_MIN + 0x0600)
#define HELPID_BELL_XMISSION_PSI                (HELPID_BELL + 0x0002)
#define HELPID_BELL_XMISSION_TEMP               (HELPID_BELL + 0x0003)
#define HELPID_BELL_GEN_LOAD                    (HELPID_BELL + 0x0005)
#define HELPID_BELL_TURBINE_TEMP                (HELPID_BELL + 0x0008)
#define HELPID_BELL_GAS_PRODUCER                (HELPID_BELL + 0x0009)
#define HELPID_BELL_ROTOR_TURBINE               (HELPID_BELL + 0x000c)
#define HELPID_BELL_CYCLIC_FA                   (HELPID_BELL + 0x0016)
#define HELPID_BELL_CYCLIC_LR                   (HELPID_BELL + 0x0017)
#define HELPID_BELL_ANTI_TORQUE_ROTOR           (HELPID_BELL + 0x0018)
#define HELPID_BELL_FUEL_VALVE                  (HELPID_BELL + 0x0019)
#define HELPID_BELL_GOVERNOR                    (HELPID_BELL + 0x001A)
#define HELPID_BELL_THROTTLE                    (HELPID_BELL + 0x001B)
#define HELPID_LSO                              (HELPID_MIN + 0x0780)
#define HELPID_LSO_WINDOW                       (HELPID_LSO + 0x00)
#define HELPID_CONCORDE                         (HELPID_MIN +  0x0A00)
#define HELPID_CONCORDE_ACCELEROMETER           (HELPID_CONCORDE + 0x0000)
#define HELPID_CONCORDE_ANGLE_OF_ATTACK         (HELPID_CONCORDE + 0x0001)
#define HELPID_CONCORDE_SPEED_SELECT            (HELPID_CONCORDE + 0x0002)
#define HELPID_CONCORDE_AIRSPEED_MODE_SWITCH    (HELPID_CONCORDE + 0x0003)
#define HELPID_CONCORDE_DISCREPANCY_LIGHT       (HELPID_CONCORDE + 0x0004)
#define HELPID_CONCORDE_MACHMETER               (HELPID_CONCORDE + 0x0005)
#define HELPID_CONCORDE_ALTIMETER_MODE_SWITCH   (HELPID_CONCORDE + 0x0006)
#define HELPID_CONCORDE_ATTITUDE_TEST           (HELPID_CONCORDE + 0x0007)
#define HELPID_CONCORDE_CHECK_ATT               (HELPID_CONCORDE + 0x0008)
#define HELPID_CONCORDE_ATTITUDE_DH             (HELPID_CONCORDE + 0x0009)
#define HELPID_CONCORDE_PITCH_INDEX_KNOB        (HELPID_CONCORDE + 0x000A)
#define HELPID_CONCORDE_HSI_FAIL                (HELPID_CONCORDE + 0x000B)
#define HELPID_CONCORDE_HSI_SWITCH              (HELPID_CONCORDE + 0x000C)
#define HELPID_CONCORDE_HSI_MILES               (HELPID_CONCORDE + 0x000D)
#define HELPID_CONCORDE_HSI_SPEED               (HELPID_CONCORDE + 0x000E)
#define HELPID_CONCORDE_SIDESLIP                (HELPID_CONCORDE + 0x000F)
#define HELPID_CONCORDE_DME_DISTANCE            (HELPID_CONCORDE + 0x0010)
#define HELPID_CONCORDE_DME_SPEED               (HELPID_CONCORDE + 0x0011)
#define HELPID_CONCORDE_DME_TIME                (HELPID_CONCORDE + 0x0012)
#define HELPID_CONCORDE_DME_SWITCH              (HELPID_CONCORDE + 0x0013)
#define HELPID_CONCORDE_RADIO_ALTIMETER         (HELPID_CONCORDE + 0x0014)
#define HELPID_CONCORDE_RADIO_ALTIMETER_TEST    (HELPID_CONCORDE + 0x0015)
#define HELPID_CONCORDE_RADIO_ALTIMETER_KNOB    (HELPID_CONCORDE + 0x0016)
#define HELPID_CONCORDE_TOTAL_FUEL              (HELPID_CONCORDE + 0x0017)
#define HELPID_CONCORDE_CENTER_GRAVITY          (HELPID_CONCORDE + 0x0018)
#define HELPID_CONCORDE_CENTER_GRAVITY_LIGHT    (HELPID_CONCORDE + 0x0019)
#define HELPID_CONCORDE_TEMPERATURE_TOTAL       (HELPID_CONCORDE + 0x001A)
#define HELPID_CONCORDE_TEMPERATURE_STATIC      (HELPID_CONCORDE + 0x001B)
#define HELPID_CONCORDE_TEMPERATURE_ISA         (HELPID_CONCORDE + 0x001C)
#define HELPID_CONCORDE_GMT                     (HELPID_CONCORDE + 0x001D)
#define HELPID_CONCORDE_TIMER                   (HELPID_CONCORDE + 0x001E)
#define HELPID_CONCORDE_CLOCK_MODE_SWITCH       (HELPID_CONCORDE + 0x001F)
#define HELPID_CONCORDE_CLOCK_START_STOP        (HELPID_CONCORDE + 0x0020)
#define HELPID_CONCORDE_CLOCK_FAST_SLOW         (HELPID_CONCORDE + 0x0021)
#define HELPID_CONCORDE_ENGINE_MANAGE_LIGHTS    (HELPID_CONCORDE + 0x0022)
#define HELPID_CONCORDE_ENGINE_MANAGE_GO        (HELPID_CONCORDE + 0x0023)
#define HELPID_CONCORDE_ENGINE_MANAGE_FAILURE   (HELPID_CONCORDE + 0x0024)
#define HELPID_CONCORDE_ENGINE_MANAGE_REVERSE   (HELPID_CONCORDE + 0x0025)
#define HELPID_CONCORDE_ENGINE_FUEL             (HELPID_CONCORDE + 0x0026)
#define HELPID_CONCORDE_ENGINE_FUEL_SETTING_KNOB (HELPID_CONCORDE + 0x0027)
#define HELPID_CONCORDE_ENGINE_EGT              (HELPID_CONCORDE + 0x0028)
#define HELPID_CONCORDE_ENGINE_AREA             (HELPID_CONCORDE + 0x0029)
#define HELPID_CONCORDE_ENGINE_FAILURE_LIGHT    (HELPID_CONCORDE + 0x002A)
#define HELPID_CONCORDE_ENGINE_AFTERBURNER_LIGHT (HELPID_CONCORDE + 0x002B)
#define HELPID_CONCORDE_VISOR_NOSE_LEVER        (HELPID_CONCORDE + 0x002C)
#define HELPID_CONCORDE_VISOR_INDICATOR         (HELPID_CONCORDE + 0x002D)
#define HELPID_CONCORDE_NOSE_INDICATOR          (HELPID_CONCORDE + 0x002E)
#define HELPID_CONCORDE_LEFT_GEAR_UNLOCKED      (HELPID_CONCORDE + 0x002F)
#define HELPID_CONCORDE_RIGHT_GEAR_UNLOCKED     (HELPID_CONCORDE + 0x0030)
#define HELPID_CONCORDE_UP_GEAR_UNLOCKED        (HELPID_CONCORDE + 0x0031)
#define HELPID_CONCORDE_GEAR_TRANSIT            (HELPID_CONCORDE + 0x0032)
#define HELPID_CONCORDE_LEFT_GEAR_DOWN          (HELPID_CONCORDE + 0x0033)
#define HELPID_CONCORDE_NOSE_GEAR_DOWN          (HELPID_CONCORDE + 0x0034)
#define HELPID_CONCORDE_TAIL_GEAR_DOWN          (HELPID_CONCORDE + 0x0035)
#define HELPID_CONCORDE_RIGHT_GEAR_DOWN         (HELPID_CONCORDE + 0x0036)
#define HELPID_CONCORDE_CONTROL_SURFACE         (HELPID_CONCORDE + 0x0037)
#define HELPID_CONCORDE_CONTROL_SURFACE_RESET   (HELPID_CONCORDE + 0x0038)
#define HELPID_CONCORDE_CONTROL_SURFACE_TEST    (HELPID_CONCORDE + 0x0039)
#define HELPID_CONCORDE_CONTROL_SURFACE_LIGHTS  (HELPID_CONCORDE + 0x003A)
#define HELPID_CONCORDE_TURB_MODE               (HELPID_CONCORDE + 0x003B)
#define HELPID_CONCORDE_MAX_CLIMB               (HELPID_CONCORDE + 0x003C)
#define HELPID_CONCORDE_MAX_CRUISE              (HELPID_CONCORDE + 0x003D)
#define HELPID_CONCORDE_MASTER_WARNING          (HELPID_CONCORDE + 0x003E)
#define HELPID_CONCORDE_FUEL_QUANTITY           (HELPID_CONCORDE + 0x003F)
#define HELPID_CONCORDE_LOW_FUEL_LIGHT          (HELPID_CONCORDE + 0x0040)
#define HELPID_CONCORDE_FUEL_CROSSFEED          (HELPID_CONCORDE + 0x0041)
#define HELPID_CONCORDE_FUEL_PUMP               (HELPID_CONCORDE + 0x0042)
#define HELPID_CONCORDE_MASTER_SWITCH           (HELPID_CONCORDE + 0x0043)
#define HELPID_CONCORDE_ENGINE_START            (HELPID_CONCORDE + 0x0044)
#define HELPID_CONCORDE_GENERATOR               (HELPID_CONCORDE + 0x0045)
#define HELPID_CONCORDE_THROTTLES               (HELPID_CONCORDE + 0x0046)
#define HELPID_CONCORDE_REHEAT                  (HELPID_CONCORDE + 0x0047)
#define HELPID_CONCORDE_THROTTLE_FAILURE        (HELPID_CONCORDE + 0x0048)
#define HELPID_CONCORDE_BRAKE                   (HELPID_CONCORDE + 0x0049)
#define HELPID_CONCORDE_WARNING_CG              (HELPID_CONCORDE + 0x004A)
#define HELPID_CONCORDE_WARNING_ELECTRICAL      (HELPID_CONCORDE + 0x004B)
#define HELPID_CONCORDE_WARNING_ENGINE          (HELPID_CONCORDE + 0x004C)
#define HELPID_CONCORDE_WARNING_FUEL            (HELPID_CONCORDE + 0x004D)
#define HELPID_CONCORDE_WARNING_ICE             (HELPID_CONCORDE + 0x004E)
#define HELPID_CONCORDE_WARNING_AIR             (HELPID_CONCORDE + 0x004F)
#define HELPID_CONCORDE_WARNING_TRIM            (HELPID_CONCORDE + 0x0050)
#define HELPID_CONCORDE_WARNING_INTAKE          (HELPID_CONCORDE + 0x0051)
#define HELPID_CONCORDE_WARNING_HYDRAULIC       (HELPID_CONCORDE + 0x0052)
#define HELPID_CONCORDE_WARNING_SMOKE           (HELPID_CONCORDE + 0x0053)
#define HELPID_CONCORDE_RADIO_ADF               (HELPID_CONCORDE + 0x0054)
#define HELPID_CONCORDE_RADIO_ADF_SET           (HELPID_CONCORDE + 0x0055)
#define HELPID_CONCORDE_RADIO_ADF_FREQUENCY     (HELPID_CONCORDE + 0x0056)
#define HELPID_CONCORDE_RADIO_ADF_IDENT         (HELPID_CONCORDE + 0x0057)
#define HELPID_CONCORDE_RADIO_ADF_TEST          (HELPID_CONCORDE + 0x0058)
#define HELPID_CONCORDE_RADIO_COMM1             (HELPID_CONCORDE + 0x0059)
#define HELPID_CONCORDE_RADIO_COMM2             (HELPID_CONCORDE + 0x005A)
#define HELPID_CONCORDE_RADIO_COMM_FREQUENCY    (HELPID_CONCORDE + 0x005B)
#define HELPID_CONCORDE_RADIO_COMM_FREQUENCY_STBY (HELPID_CONCORDE + 0x005C)
#define HELPID_CONCORDE_RADIO_COMM_SET          (HELPID_CONCORDE + 0x005D)
#define HELPID_CONCORDE_RADIO_COMM_SET_STBY     (HELPID_CONCORDE + 0x005E)
#define HELPID_CONCORDE_RADIO_COMM_LIGHT        (HELPID_CONCORDE + 0x005F)
#define HELPID_CONCORDE_RADIO_COMM_SWITCH       (HELPID_CONCORDE + 0x0060)
#define HELPID_CONCORDE_RADIO_TRANSPONDER       (HELPID_CONCORDE + 0x0061)
#define HELPID_CONCORDE_RADIO_TRANSPONDER_ID    (HELPID_CONCORDE + 0x0062)
#define HELPID_CONCORDE_RADIO_TRANSPONDER_SET_ID (HELPID_CONCORDE + 0x0063)
#define HELPID_CONCORDE_RADIO_TRANSPONDER_MODE  (HELPID_CONCORDE + 0x0064)
#define HELPID_CONCORDE_RADIO_TRANSPONDER_POSITION (HELPID_CONCORDE + 0x0065)
#define HELPID_CONCORDE_RADIO_LIGHTS            (HELPID_CONCORDE + 0x0066)
#define HELPID_CONCORDE_BATTERY_SWITCH          (HELPID_CONCORDE + 0x0067)
#define HELPID_CONCORDE_AVIONICS_SWITCH         (HELPID_CONCORDE + 0x0068)
#define HELPID_CONCORDE_THROTTLES_REVERSE       (HELPID_CONCORDE + 0x0069)
#define HELPID_CONCORDE_VOR_BEARING             (HELPID_CONCORDE + 0x006A)
#define HELPID_CONCORDE_LIGHT_SWITCH_ANTI_COLLISION (HELPID_CONCORDE + 0x006B)
#define HELPID_777                              (HELPID_MIN +  0x0C00)
#define HELPID_777_AUTOPILOT                    (HELPID_777 + 0x0000)
#define HELPID_777_ENGINE_AUTOSTART             (HELPID_777 + 0x0001)
#define HELPID_MOONEY                           (HELPID_MIN +  0x0C80)
#define HELPID_MOONEY_STANDBY_VAC_SWITCH        (HELPID_MOONEY + 0x0000)
#define HELPID_MOONEY_PROP_DEICE                (HELPID_MOONEY + 0x0001)
#define HELPID_MOONEY_SPEED_BRAKE               (HELPID_MOONEY + 0x0002)
#define HELPID_KINGAIR                          (HELPID_MIN +  0x0D00)
#define HELPID_KINGAIR_AUTOFEATHER_LIGHT        (HELPID_KINGAIR + 0x0000)
#define HELPID_KINGAIR_AUTOFEATHER_SWITCH       (HELPID_KINGAIR + 0x0001)
#define HELPID_KINGAIR_PROP_SYNC_SWITCH         (HELPID_KINGAIR + 0x0002)
#define HELPID_KINGAIR_PROP_SYNCHROSCOPE        (HELPID_KINGAIR + 0x0003)
#define HELPID_KINGAIR_PROP_LEVERS              (HELPID_KINGAIR + 0x0004)
#define HELPID_KINGAIR_CONDITION_LEVERS         (HELPID_KINGAIR + 0x0005)
#define HELPID_CFS                              (HELPID_MIN +  0x0800)
#define HELPID_SPITFIRE_ENGINE_TEMP             (HELPID_CFS + 0)
#define HELPID_SPITFIRE_GEAR                    (HELPID_CFS + 1)
#define HELPID_SPITFIRE_OIL_TEMP                (HELPID_CFS + 2)
#define HELPID_SPITFIRE_STARTER                 (HELPID_CFS + 3)
#define HELPID_SPITFIRE_MAGNETO                 (HELPID_CFS + 4)
#define HELPID_SPITFIRE_OIL_PRESSURE            (HELPID_CFS + 5)
#define HELPID_SPITFIRE_FLAPS                   (HELPID_CFS + 6)
#define HELPID_SPITFIRE_CLOCK                   (HELPID_CFS + 8)
#define HELPID_SPITFIRE_AIRSPEED                (HELPID_CFS + 9)
#define HELPID_SPITFIRE_ATTITUDE                (HELPID_CFS + 10)
#define HELPID_SPITFIRE_VERTICAL_SPEED          (HELPID_CFS + 11)
#define HELPID_SPITFIRE_ALTIMETER               (HELPID_CFS + 12)
#define HELPID_SPITFIRE_DG                      (HELPID_CFS + 13)
#define HELPID_SPITFIRE_TURN_BANK               (HELPID_CFS + 14)
#define HELPID_SPITFIRE_WARNING_LIGHT           (HELPID_CFS + 15)
#define HELPID_SPITFIRE_BOOST_PRESSURE          (HELPID_CFS + 16)
#define HELPID_SPITFIRE_FUEL_SELECTOR           (HELPID_CFS + 17)
#define HELPID_SPITFIRE_TACHOMETER              (HELPID_CFS + 18)
#define HELPID_SPITFIRE_FUEL                    (HELPID_CFS + 19)
#define HELPID_SPITFIRE_THROTTLE                (HELPID_CFS + 20)
#define HELPID_SPITFIRE_MIXTURE                 (HELPID_CFS + 21)
#define HELPID_SPITFIRE_PROP                    (HELPID_CFS + 22)
#define HELPID_FW190A_GEAR                      (HELPID_CFS + 50)
#define HELPID_FW190A_CLOCK                     (HELPID_CFS + 51)
#define HELPID_FW190A_OIL_TEMP                  (HELPID_CFS + 52)
#define HELPID_FW190A_ATTITUDE                  (HELPID_CFS + 53)
#define HELPID_FW190A_DG                        (HELPID_CFS + 54)
#define HELPID_FW190A_OIL_PRESSURE              (HELPID_CFS + 55)
#define HELPID_FW190A_FUEL                      (HELPID_CFS + 56)
#define HELPID_FW190A_WARNING_LIGHT             (HELPID_CFS + 57)
#define HELPID_FW190A_STARTER                   (HELPID_CFS + 58)
#define HELPID_FW190A_FLAPS                     (HELPID_CFS + 59)
#define HELPID_FW190A_AIRSPEED                  (HELPID_CFS + 60)
#define HELPID_FW190A_ALTIMETER                 (HELPID_CFS + 61)
#define HELPID_FW190A_VERTICAL_SPEED            (HELPID_CFS + 62)
#define HELPID_FW190A_TACHOMETER                (HELPID_CFS + 63)
#define HELPID_FW190A_BOOST_PRESSURE            (HELPID_CFS + 64)
#define HELPID_FW190A_FUEL_SELECTOR             (HELPID_CFS + 65)
#define HELPID_FW190A_THROTTLE                  (HELPID_CFS + 66)
#define HELPID_FW190A_MIXTURE                   (HELPID_CFS + 67)
#define HELPID_FW190A_PROP                      (HELPID_CFS + 68)
#define HELPID_P51_FLAPS                        (HELPID_CFS + 100)
#define HELPID_P51_MAGNETO                      (HELPID_CFS + 101)
#define HELPID_P51_GEAR                         (HELPID_CFS + 102)
#define HELPID_P51_STARTER                      (HELPID_CFS + 103)
#define HELPID_P51_CLOCK                        (HELPID_CFS + 104)
#define HELPID_P51_ENGINE_TEMP                  (HELPID_CFS + 106)
#define HELPID_P51_AIRSPEED                     (HELPID_CFS + 107)
#define HELPID_P51_DG                           (HELPID_CFS + 108)
#define HELPID_P51_ATTITUDE                     (HELPID_CFS + 109)
#define HELPID_P51_MANIFOLD_PRESSURE            (HELPID_CFS + 110)
#define HELPID_P51_ALTIMETER                    (HELPID_CFS + 111)
#define HELPID_P51_TURN_BANK                    (HELPID_CFS + 112)
#define HELPID_P51_VERTICAL_SPEED               (HELPID_CFS + 113)
#define HELPID_P51_TACHOMETER                   (HELPID_CFS + 114)
#define HELPID_P51_FUEL_SELECTOR                (HELPID_CFS + 115)
#define HELPID_P51_FUEL                         (HELPID_CFS + 116)
#define HELPID_P51_ENGINE                       (HELPID_CFS + 117)
#define HELPID_P51_THROTTLE                     (HELPID_CFS + 118)
#define HELPID_P51_MIXTURE                      (HELPID_CFS + 119)
#define HELPID_P51_PROP                         (HELPID_CFS + 120)
#define HELPID_BF109_STARTER                    (HELPID_CFS + 150)
#define HELPID_BF109_MAGNETO                    (HELPID_CFS + 151)
#define HELPID_BF109_GEAR                       (HELPID_CFS + 152)
#define HELPID_BF109_FLAPS                      (HELPID_CFS + 153)
#define HELPID_BF109_CLOCK                      (HELPID_CFS + 154)
#define HELPID_BF109_GUNSIGHT                   (HELPID_CFS + 155)
#define HELPID_BF109_ALTIMETER                  (HELPID_CFS + 156)
#define HELPID_BF109_DG                         (HELPID_CFS + 157)
#define HELPID_BF109_TACHOMETER                 (HELPID_CFS + 158)
#define HELPID_BF109_AIRSPEED                   (HELPID_CFS + 159)
#define HELPID_BF109_ENGINE_TEMP                (HELPID_CFS + 160)
#define HELPID_BF109_TURN_BANK                  (HELPID_CFS + 161)
#define HELPID_BF109_WARNING_LIGHT              (HELPID_CFS + 162)
#define HELPID_BF109_BOOST_PRESSURE             (HELPID_CFS + 163)
#define HELPID_BF109_OIL_TEMP                   (HELPID_CFS + 164)
#define HELPID_BF109_FUEL                       (HELPID_CFS + 165)
#define HELPID_BF109_FUEL_OIL_PRESSURE          (HELPID_CFS + 166)
#define HELPID_BF109_THROTTLE                   (HELPID_CFS + 167)
#define HELPID_BF109_MIXTURE                    (HELPID_CFS + 168)
#define HELPID_BF109_PROP                       (HELPID_CFS + 169)
#define HELPID_HURRICANE_GEAR                   (HELPID_CFS + 200)
#define HELPID_HURRICANE_FUEL_SELECTOR          (HELPID_CFS + 201)
#define HELPID_HURRICANE_FUEL                   (HELPID_CFS + 202)
#define HELPID_HURRICANE_ENGINE_TEMP            (HELPID_CFS + 203)
#define HELPID_HURRICANE_STARTER                (HELPID_CFS + 204)
#define HELPID_HURRICANE_MAGNETO                (HELPID_CFS + 205)
#define HELPID_HURRICANE_WARNING_LIGHT          (HELPID_CFS + 206)
#define HELPID_HURRICANE_CLOCK                  (HELPID_CFS + 207)
#define HELPID_HURRICANE_AIRSPEED               (HELPID_CFS + 208)
#define HELPID_HURRICANE_ATTITUDE               (HELPID_CFS + 209)
#define HELPID_HURRICANE_VERTICAL_SPEED         (HELPID_CFS + 210)
#define HELPID_HURRICANE_ALTIMETER              (HELPID_CFS + 211)
#define HELPID_HURRICANE_DG                     (HELPID_CFS + 212)
#define HELPID_HURRICANE_TURN_BANK              (HELPID_CFS + 213)
#define HELPID_HURRICANE_TACHOMETER             (HELPID_CFS + 214)
#define HELPID_HURRICANE_OIL_PRESSURE           (HELPID_CFS + 215)
#define HELPID_HURRICANE_BOOST_PRESSURE         (HELPID_CFS + 216)
#define HELPID_HURRICANE_OIL_TEMP               (HELPID_CFS + 217)
#define HELPID_HURRICANE_FLAPS                  (HELPID_CFS + 218)
#define HELPID_HURRICANE_THROTTLE               (HELPID_CFS + 219)
#define HELPID_HURRICANE_MIXTURE                (HELPID_CFS + 220)
#define HELPID_HURRICANE_PROP                   (HELPID_CFS + 221)
#define HELPID_P47_FUEL_SELECTOR                (HELPID_CFS + 250)
#define HELPID_P47_FUEL                         (HELPID_CFS + 251)
#define HELPID_P47_WARNING_LIGHT                (HELPID_CFS + 252)
#define HELPID_P47_CLOCK                        (HELPID_CFS + 253)
#define HELPID_P47_AIRSPEED                     (HELPID_CFS + 254)
#define HELPID_P47_TURN_BANK                    (HELPID_CFS + 255)
#define HELPID_P47_ATTITUDE                     (HELPID_CFS + 256)
#define HELPID_P47_TACHOMETER                   (HELPID_CFS + 257)
#define HELPID_P47_ENGINE                       (HELPID_CFS + 258)
#define HELPID_P47_FLAPS_GEAR                   (HELPID_CFS + 259)
#define HELPID_P47_STARTER                      (HELPID_CFS + 260)
#define HELPID_P47_MAGNETO                      (HELPID_CFS + 261)
#define HELPID_P47_ALTIMETER                    (HELPID_CFS + 262)
#define HELPID_P47_DG                           (HELPID_CFS + 263)
#define HELPID_P47_VERTICAL_SPEED               (HELPID_CFS + 264)
#define HELPID_P47_MANIFOLD_PRESSURE            (HELPID_CFS + 265)
#define HELPID_P47_ENGINE_TEMP                  (HELPID_CFS + 266)
#define HELPID_P47_THROTTLE                     (HELPID_CFS + 267)
#define HELPID_P47_MIXTURE                      (HELPID_CFS + 268)
#define HELPID_P47_PROP                         (HELPID_CFS + 269)
#define HELPID_WILDCAT_PROP                     (HELPID_CFS + 270)
#define HELPID_WILDCAT_FUEL_SELECTOR            (HELPID_CFS + 271)

#define HELPID_RADIO                            (HELPID_MIN + 0x0700)
#define HELPID_RADIO_NAV1                       (HELPID_RADIO + 0x0000)
#define HELPID_RADIO_COMM1                      (HELPID_RADIO + 0x0001)
#define HELPID_RADIO_NAV2                       (HELPID_RADIO + 0x0002)
#define HELPID_RADIO_COMM2                      (HELPID_RADIO + 0x0003)
#define HELPID_RADIO_DME                        (HELPID_RADIO + 0x0004)
#define HELPID_RADIO_DME_KT                     (HELPID_RADIO + 0x0005)
#define HELPID_RADIO_DME_NM                     (HELPID_RADIO + 0x0006)
#define HELPID_RADIO_DME_MIN                    (HELPID_RADIO + 0x0007)
#define HELPID_RADIO_DME_N1_N2_SELECT           (HELPID_RADIO + 0x0008)
#define HELPID_RADIO_ADF_RADIO                  (HELPID_RADIO + 0x0009)
#define HELPID_RADIO_TRANSPONDER                (HELPID_RADIO + 0x000a)
#define HELPID_RADIO_RADIOS_OPEN                (HELPID_RADIO + 0x000b)
#define HELPID_RADIO_AP_RADIO                   (HELPID_RADIO + 0x000c)
#define HELPID_RADIO_SWITCH_STANDBY_FREQUENCY   (HELPID_RADIO + 0x000d)
#define HELPID_RADIO_COMM_SELECT                (HELPID_RADIO + 0x00e)
#define HELPID_RADIO_COMM_FREQ                  (HELPID_RADIO + 0x000f)
#define HELPID_RADIO_COMM1_SELECT               (HELPID_RADIO + 0x0010)
#define HELPID_RADIO_COMM1_FREQ                 (HELPID_RADIO + 0x0011)
#define HELPID_RADIO_COMM2_SELECT               (HELPID_RADIO + 0x0012)
#define HELPID_RADIO_COMM2_FREQ                 (HELPID_RADIO + 0x0013)
#define HELPID_RADIO_NAV1_SELECT                (HELPID_RADIO + 0x0014)
#define HELPID_RADIO_NAV1_FREQ                  (HELPID_RADIO + 0x0015)
#define HELPID_RADIO_NAV2_SELECT                (HELPID_RADIO + 0x0016)
#define HELPID_RADIO_NAV2_FREQ                  (HELPID_RADIO + 0x0017)
#define HELPID_RADIO_ADF                        (HELPID_RADIO + 0x0018)
#define HELPID_RADIO_AUTOPILOT                  (HELPID_RADIO + 0x0019)
#define HELPID_RADIO_OPEN_STACK                 (HELPID_RADIO + 0x001a)
#define HELPID_RADIO_DME_IND                    (HELPID_RADIO + 0x001B)
#define HELPID_GPS_MODE                         (HELPID_RADIO + 0x001C)
#define HELPID_GPS_DIRECT                       (HELPID_RADIO + 0x001D)
#define HELPID_GPS_MENU                         (HELPID_RADIO + 0x001E)
#define HELPID_GPS_ARROWS                       (HELPID_RADIO + 0x001F)
#define HELPID_GPS_ENTER                        (HELPID_RADIO + 0x0020)
#define HELPID_GPS_CANCEL                       (HELPID_RADIO + 0x0021)
#define HELPID_RADIO_AUDIO                      (HELPID_RADIO + 0x0022)
#define HELPID_AUTOPILOT                        (HELPID_MIN +  0x0D80)
#define HELPID_AUTOPILOT_WING_LEVELER           (HELPID_AUTOPILOT + 0x0000)
#define HELPID_AUTOPILOT_IAS_HOLD               (HELPID_AUTOPILOT + 0x0001)
#define HELPID_AUTOPILOT_BC_HOLD                (HELPID_AUTOPILOT + 0x0002)
#define HELPID_AUTOPILOT_APP_HOLD               (HELPID_AUTOPILOT + 0x0003)
#define HELPID_AUTOPILOT_YAW_DAMP_HOLD          (HELPID_AUTOPILOT + 0x0004)
#define HELPID_AUTOPILOT_NAV_HOLD               (HELPID_AUTOPILOT + 0x0005)
#define HELPID_AUTOPILOT_NAV_INDICATOR          (HELPID_AUTOPILOT + 0x0006)
#define HELPID_AUTOPILOT_HEADING_HOLD           (HELPID_AUTOPILOT + 0x0007)
#define HELPID_AUTOPILOT_HEADING_INDICATOR      (HELPID_AUTOPILOT + 0x0008)
#define HELPID_AUTOPILOT_IAS_ACQUIRE_HOLD       (HELPID_AUTOPILOT + 0x0009)
#define HELPID_AUTOPILOT_VSI_HOLD               (HELPID_AUTOPILOT + 0x000A)
#define HELPID_AUTOPILOT_VSI_INDICATOR          (HELPID_AUTOPILOT + 0x000B)
#define HELPID_AUTOPILOT_ALTITUDE_HOLD          (HELPID_AUTOPILOT + 0x000C)
#define HELPID_AUTOPILOT_ALTITUDE_INDICATOR     (HELPID_AUTOPILOT + 0x000D)
#define HELPID_AUTOPILOT_SPEED_HOLD             (HELPID_AUTOPILOT + 0x000E)
#define HELPID_AUTOPILOT_SPEED_INDICATOR        (HELPID_AUTOPILOT + 0x000F)
#define HELPID_AUTOPILOT_FLIGHT_DIRECTOR_SWITCH (HELPID_AUTOPILOT + 0x0010)
#define HELPID_AUTOPILOT_AUTOTHROTTLE           (HELPID_AUTOPILOT + 0x0011)
#define HELPID_AUTOPILOT_MACH_HOLD              (HELPID_AUTOPILOT + 0x0012)
#define HELPID_AUTOPILOT_MACH_INDICATOR         (HELPID_AUTOPILOT + 0x0013)
#define HELPID_AUTOPILOT_VSI_WHEEL              (HELPID_AUTOPILOT + 0x0014)

#define HELPID_GAUGE                            (HELPID_MIN +  0x0B00)
#define HELPID_GAUGE_ADF                        (HELPID_GAUGE + 0x0000)
#define HELPID_GAUGE_ADF_SET                    (HELPID_GAUGE + 0x0001)
#define HELPID_GAUGE_AIRSPEED                   (HELPID_GAUGE + 0x0002)
#define HELPID_GAUGE_AIRSPEED_STRIP             (HELPID_GAUGE + 0x0003)
#define HELPID_GAUGE_ALTERNATOR_SWITCH          (HELPID_GAUGE + 0x0004)
#define HELPID_GAUGE_ALTERNATOR_SWITCH_LEFT     (HELPID_GAUGE + 0x0005)
#define HELPID_GAUGE_ALTERNATOR_SWITCH_RIGHT    (HELPID_GAUGE + 0x0006)
#define HELPID_GAUGE_ALTIMETER                  (HELPID_GAUGE + 0x0007)
#define HELPID_GAUGE_ALTIMETER_SET              (HELPID_GAUGE + 0x0008)
#define HELPID_GAUGE_ALTIMETER_STRIP            (HELPID_GAUGE + 0x0009)
#define HELPID_GAUGE_ALTITUDE_ALERTER           (HELPID_GAUGE + 0x000A)
#define HELPID_GAUGE_AMMETER                    (HELPID_GAUGE + 0x000B)
#define HELPID_GAUGE_ANNUNCIATOR                (HELPID_GAUGE + 0x000C)
#define HELPID_GAUGE_ATTITUDE_INDICATOR         (HELPID_GAUGE + 0x000D)
#define HELPID_GAUGE_AUTOPILOT_MASTER           (HELPID_GAUGE + 0x000E)
#define HELPID_GAUGE_AVIONICS_SWITCH            (HELPID_GAUGE + 0x000F)
#define HELPID_GAUGE_BACKUP_AIRSPEED            (HELPID_GAUGE + 0x0010)
#define HELPID_GAUGE_BACKUP_ALTIMETER           (HELPID_GAUGE + 0x0011)
#define HELPID_GAUGE_BACKUP_ATTITUDE            (HELPID_GAUGE + 0x0012)
#define HELPID_GAUGE_BATTERY_SWITCH             (HELPID_GAUGE + 0x0013)
#define HELPID_GAUGE_BOOST_PUMP_SWITCH          (HELPID_GAUGE + 0x0014)
#define HELPID_GAUGE_CHT                        (HELPID_GAUGE + 0x0015)
#define HELPID_GAUGE_CLOCK                      (HELPID_GAUGE + 0x0016)
#define HELPID_GAUGE_COWL_FLAPS                 (HELPID_GAUGE + 0x0017)
#define HELPID_GAUGE_COWL_FLAPS_INDICATOR       (HELPID_GAUGE + 0x0018)
#define HELPID_GAUGE_COWL_FLAPS_SWITCH          (HELPID_GAUGE + 0x0019)
#define HELPID_GAUGE_DEICE_SWITCH               (HELPID_GAUGE + 0x001A)
#define HELPID_GAUGE_DME1                       (HELPID_GAUGE + 0x001B)
#define HELPID_GAUGE_DME2                       (HELPID_GAUGE + 0x001C)
#define HELPID_GAUGE_EGT                        (HELPID_GAUGE + 0x001D)
#define HELPID_GAUGE_EICAS                      (HELPID_GAUGE + 0x001E)
#define HELPID_GAUGE_ELAPSED_TIME               (HELPID_GAUGE + 0x001F)
#define HELPID_GAUGE_ENGINE_CONTROLS            (HELPID_GAUGE + 0x0020)
#define HELPID_GAUGE_ENGINE_N1                  (HELPID_GAUGE + 0x0021)
#define HELPID_GAUGE_ENGINE_N2                  (HELPID_GAUGE + 0x0022)
#define HELPID_GAUGE_ENGINE_TEMPERATURE         (HELPID_GAUGE + 0x0023)
#define HELPID_GAUGE_FLAP_INDICATOR             (HELPID_GAUGE + 0x0024)
#define HELPID_GAUGE_FLAPS_SWITCH               (HELPID_GAUGE + 0x0025)
#define HELPID_GAUGE_FUEL                       (HELPID_GAUGE + 0x0026)
#define HELPID_GAUGE_FUEL_CUTOFF                (HELPID_GAUGE + 0x0027)
#define HELPID_GAUGE_FUEL_FLOW                  (HELPID_GAUGE + 0x0028)
#define HELPID_GAUGE_FUEL_LEFT                  (HELPID_GAUGE + 0x0029)
#define HELPID_GAUGE_FUEL_RIGHT                 (HELPID_GAUGE + 0x002A)
#define HELPID_GAUGE_FUEL_LOW_LIGHT             (HELPID_GAUGE + 0x002B)
#define HELPID_GAUGE_FUEL_PRESSURE              (HELPID_GAUGE + 0x002C)
#define HELPID_GAUGE_FUEL_SELECTOR              (HELPID_GAUGE + 0x002D)
#define HELPID_GAUGE_FUEL_XFEED                 (HELPID_GAUGE + 0x002E)
#define HELPID_GAUGE_GAS_GENERATOR_RPM          (HELPID_GAUGE + 0x002F)
#define HELPID_GAUGE_GEAR_LEVER                 (HELPID_GAUGE + 0x0030)
#define HELPID_GAUGE_GEAR_LIGHT                 (HELPID_GAUGE + 0x0031)
#define HELPID_GAUGE_GLIDE_SLOPE                (HELPID_GAUGE + 0x0032)
#define HELPID_GAUGE_HEADING_BUG_ADJUST         (HELPID_GAUGE + 0x0033)
#define HELPID_GAUGE_HEADING_INDICATOR          (HELPID_GAUGE + 0x0034)
#define HELPID_GAUGE_HEADING_INDICATOR_ADJUST   (HELPID_GAUGE + 0x0035)
#define HELPID_GAUGE_HSI                        (HELPID_GAUGE + 0x0036)
#define HELPID_GAUGE_HSI_HUD                    (HELPID_GAUGE + 0x0037)
#define HELPID_GAUGE_HSI_OBS1                   (HELPID_GAUGE + 0x0038)
#define HELPID_GAUGE_IOAT                       (HELPID_GAUGE + 0x0039)
#define HELPID_GAUGE_KOHLSMAN_KNOB              (HELPID_GAUGE + 0x003A)
#define HELPID_GAUGE_LIGHT_AP_INDICATOR         (HELPID_GAUGE + 0x003B)
#define HELPID_GAUGE_LIGHT_MASTER_WARNING       (HELPID_GAUGE + 0x003C)
#define HELPID_GAUGE_LIGHT_SWITCH_BEACON        (HELPID_GAUGE + 0x003D)
#define HELPID_GAUGE_LIGHT_SWITCH_LANDING       (HELPID_GAUGE + 0x003E)
#define HELPID_GAUGE_LIGHT_SWITCH_NAV           (HELPID_GAUGE + 0x003F)
#define HELPID_GAUGE_LIGHT_SWITCH_PANEL         (HELPID_GAUGE + 0x0040)
#define HELPID_GAUGE_LIGHT_SWITCH_RECOGNITION   (HELPID_GAUGE + 0x0041)
#define HELPID_GAUGE_LIGHT_SWITCH_STROBE        (HELPID_GAUGE + 0x0042)
#define HELPID_GAUGE_LIGHT_SWITCH_TAXI          (HELPID_GAUGE + 0x0043)
#define HELPID_GAUGE_LOCALIZER_INDEX            (HELPID_GAUGE + 0x0044)
#define HELPID_GAUGE_MAGNETO_SWITCH             (HELPID_GAUGE + 0x0045)
#define HELPID_GAUGE_MANIFOLD_PRESSURE          (HELPID_GAUGE + 0x0046)
#define HELPID_GAUGE_MARKER_OUTER               (HELPID_GAUGE + 0x0047)
#define HELPID_GAUGE_MARKER_MIDDLE              (HELPID_GAUGE + 0x0048)
#define HELPID_GAUGE_MARKER_INNER               (HELPID_GAUGE + 0x0049)
#define HELPID_GAUGE_MASTER_SWITCH              (HELPID_GAUGE + 0x004A)
#define HELPID_GAUGE_MASTER_ALT_BAT             (HELPID_GAUGE + 0x004B)
#define HELPID_GAUGE_MASTER_CAUTION_LIGHT       (HELPID_GAUGE + 0x004C)
#define HELPID_GAUGE_NAV_GPS_SWITCH             (HELPID_GAUGE + 0x004D)
#define HELPID_GAUGE_OIL_PRESSURE               (HELPID_GAUGE + 0x004E)
#define HELPID_GAUGE_OIL_TEMPERATURE            (HELPID_GAUGE + 0x004F)
#define HELPID_GAUGE_OMI_LIGHTS                 (HELPID_GAUGE + 0x0050)
#define HELPID_GAUGE_PARKING_BRAKE              (HELPID_GAUGE + 0x0051)
#define HELPID_GAUGE_PFD                        (HELPID_GAUGE + 0x0052)
#define HELPID_GAUGE_PITCH_TRIM                 (HELPID_GAUGE + 0x0053)
#define HELPID_GAUGE_PITCH_TRIM_INDICATOR       (HELPID_GAUGE + 0x0054)
#define HELPID_GAUGE_PITCH_TRIM_KNOB            (HELPID_GAUGE + 0x0055)
#define HELPID_GAUGE_PITCH_TRIM_SWITCH          (HELPID_GAUGE + 0x0056)
#define HELPID_GAUGE_PITOT_HEAT_SWITCH          (HELPID_GAUGE + 0x0057)
#define HELPID_GAUGE_PROP_RPM                   (HELPID_GAUGE + 0x0058)
#define HELPID_GAUGE_PROP_SYNC                  (HELPID_GAUGE + 0x0059)
#define HELPID_GAUGE_RMI                        (HELPID_GAUGE + 0x005A)
#define HELPID_GAUGE_RMI_DME                    (HELPID_GAUGE + 0x005B)
#define HELPID_GAUGE_ROLL_TRIM                  (HELPID_GAUGE + 0x005C)
#define HELPID_GAUGE_ROLL_TRIM_INDICATOR        (HELPID_GAUGE + 0x005D)
#define HELPID_GAUGE_ROLL_TRIM_KNOB             (HELPID_GAUGE + 0x005E)
#define HELPID_GAUGE_ROLL_TRIM_SWITCH           (HELPID_GAUGE + 0x005F)
#define HELPID_GAUGE_SIM_RATE                   (HELPID_GAUGE + 0x0060)
#define HELPID_GAUGE_SLIP_SKID                  (HELPID_GAUGE + 0x0061)
#define HELPID_GAUGE_SPOILER                    (HELPID_GAUGE + 0x0062)
#define HELPID_GAUGE_STARTER_LEFT               (HELPID_GAUGE + 0x0063)
#define HELPID_GAUGE_STARTER_RIGHT              (HELPID_GAUGE + 0x0064)
#define HELPID_GAUGE_STARTER_SWITCH             (HELPID_GAUGE + 0x0065)
#define HELPID_GAUGE_SUCTION                    (HELPID_GAUGE + 0x0066)
#define HELPID_GAUGE_SWITCH_FUEL_PUMP           (HELPID_GAUGE + 0x0067)
#define HELPID_GAUGE_TACHOMETER                 (HELPID_GAUGE + 0x0068)
#define HELPID_GAUGE_TAS_CALIBRATION            (HELPID_GAUGE + 0x0069)
#define HELPID_GAUGE_THROTTLE_PROP              (HELPID_GAUGE + 0x006A)
#define HELPID_GAUGE_THROTTLE_MIXTURE           (HELPID_GAUGE + 0x006B)
#define HELPID_GAUGE_THROTTLE_THROTTLE          (HELPID_GAUGE + 0x006C)
#define HELPID_GAUGE_THROTTLE_QUADRANT          (HELPID_GAUGE + 0x006D)
#define HELPID_GAUGE_THRUST_LEVERS              (HELPID_GAUGE + 0x006E)
#define HELPID_GAUGE_TIT                        (HELPID_GAUGE + 0x006F)
#define HELPID_GAUGE_TORQUE                     (HELPID_GAUGE + 0x0070)
#define HELPID_GAUGE_TURN_COORDINATOR           (HELPID_GAUGE + 0x0071)
#define HELPID_GAUGE_VOR1                       (HELPID_GAUGE + 0x0072)
#define HELPID_GAUGE_VOR1_OBS                   (HELPID_GAUGE + 0x0073)
#define HELPID_GAUGE_VOR2                       (HELPID_GAUGE + 0x0074)
#define HELPID_GAUGE_VOR2_OBS2                  (HELPID_GAUGE + 0x0075)
#define HELPID_GAUGE_VSI                        (HELPID_GAUGE + 0x0076)
#define HELPID_GAUGE_WHISKEY_COMPASS            (HELPID_GAUGE + 0x0077)
#define HELPID_GAUGE_YAW_TRIM                   (HELPID_GAUGE + 0x0078)
#define HELPID_GAUGE_YAW_TRIM_INDICATOR         (HELPID_GAUGE + 0x0079)
#define HELPID_GAUGE_YAW_TRIM_KNOB              (HELPID_GAUGE + 0x007A)
#define HELPID_GAUGE_YAW_TRIM_SWITCH            (HELPID_GAUGE + 0x007B)
#define HELPID_GAUGE_AUTOBRAKE_SWITCH           (HELPID_GAUGE + 0x007C)
#define HELPID_GAUGE_FLAP_LEVER                 (HELPID_GAUGE + 0x007D)
#define HELPID_GAUGE_FLAP_LEVER_SOLO            (HELPID_GAUGE + 0x007E)
#define HELPID_GAUGE_GENERATOR_SWITCH           (HELPID_GAUGE + 0x007F)
#define HELPID_GAUGE_GENERATOR_LEFT             (HELPID_GAUGE + 0x0080)
#define HELPID_GAUGE_GENERATOR_RIGHT            (HELPID_GAUGE + 0x0081)
#define HELPID_GAUGE_LIGHT_SWITCH_WING          (HELPID_GAUGE + 0x0082)
#define HELPID_GAUGE_LIGHT_SWITCH_LOGO          (HELPID_GAUGE + 0x0083)
#define HELPID_GAUGE_TURN_BANK                  (HELPID_GAUGE + 0x0084)
#define HELPID_GAUGE_VSI_KNOB                   (HELPID_GAUGE + 0x0085)
#define HELPID_GAUGE_TOGGLE_RADIO_WINDOW        (HELPID_GAUGE + 0x0086)
#define HELPID_GAUGE_TOGGLE_GPS_WINDOW          (HELPID_GAUGE + 0x0087)
#define HELPID_GAUGE_TOGGLE_THROTTLE_WINDOW     (HELPID_GAUGE + 0x0088)
#define HELPID_GAUGE_TOGGLE_COMPASS_WINDOW      (HELPID_GAUGE + 0x0089)
#define HELPID_GAUGE_TOGGLE_FUEL_WINDOW         (HELPID_GAUGE + 0x008A)
#define HELPID_GAUGE_TOGGLE_COLLECTIVE_WINDOW   (HELPID_GAUGE + 0x008B)
#define HELPID_GAUGE_HYDRAULIC_PRESSURE         (HELPID_GAUGE + 0x008C)
#define HELPID_GAUGE_COOLANT_TEMPERATURE        (HELPID_GAUGE + 0x008D)
#define HELPID_GAUGE_STALL_LIGHT                (HELPID_GAUGE + 0x008E)
#define HELPID_GAUGE_FLIGHT_MAP                 (HELPID_GAUGE + 0x008F)
#define HELPID_GAUGE_VOR2_OBS                   (HELPID_GAUGE + 0x0090)
#define HELPID_GAUGE_THROTTLE_EMERGENCY_THROTTLE (HELPID_GAUGE + 0x0091)
#define HELPID_GAUGE_WATER_RUDDER_LEVER         (HELPID_GAUGE + 0x0092)
#define HELPID_GAUGE_TOGGLE_CHECKLIST_WINDOW    (HELPID_GAUGE + 0x0093)
#define HELPID_GAUGE_TOGGLE_ATC_WINDOW          (HELPID_GAUGE + 0x0094)
#define HELPID_GAUGE_TOGGLE_OVERHEAD_WINDOW     (HELPID_GAUGE + 0x0095)
#define HELPID_GAUGE_TOGGLE_CLOCK_WINDOW        (HELPID_GAUGE + 0x0096)
#define HELPID_GAUGE_SPEED_BRAKE                (HELPID_GAUGE + 0x0097)
#define HELPID_GAUGE_BOOT_ICING_SWITCH          (HELPID_GAUGE + 0x0098)
#define HELPID_GAUGE_ANNUNCIATOR_AFT_DOOR       (HELPID_GAUGE + 0x0099)
#define HELPID_GAUGE_ANNUNCIATOR_GEAR_UP        (HELPID_GAUGE + 0x009A)
#define HELPID_GAUGE_ANNUNCIATOR_LEFT_ALTERNATOR (HELPID_GAUGE + 0x009B)
#define HELPID_GAUGE_ANNUNCIATOR_RIGHT_ALTERNATOR (HELPID_GAUGE + 0x009C)
#define HELPID_GAUGE_ANNUNCIATOR_STARTER        (HELPID_GAUGE + 0x009D)
#define HELPID_GAUGE_REF_ALT_MARKER_BUG         (HELPID_GAUGE + 0x009E)
#define HELPID_GAUGE_TOGGLE_FLOOR_WINDOW        (HELPID_GAUGE + 0x009F)
#define HELPID_GAUGE_RADIO_ALTIMETER            (HELPID_CONCORDE_RADIO_ALTIMETER)
#define HELPID_GAUGE_RADIO_ALTIMETER_KNOB       (HELPID_CONCORDE_RADIO_ALTIMETER_KNOB)
#define HELPID_GAUGE_CONDITION_LEVERS           (HELPID_KINGAIR_CONDITION_LEVERS)
#define HELPID_GAUGE_PROP_LEVERS                (HELPID_KINGAIR_PROP_LEVERS)
#define HELPID_GAUGE_DISPLAY_IFR_WINDOW         (HELPID_GAUGE + 0x00A0)
#define HELPID_GAUGE_DISPLAY_LANDING_WINDOW     (HELPID_GAUGE + 0x00A1)
#define HELPID_GAUGE_DISPLAY_VFR_WINDOW         (HELPID_GAUGE + 0x00A2)
#define HELPID_GAUGE_CLOCK_MODE_SELECT          (HELPID_GAUGE + 0x00A3)
#define HELPID_GAUGE_CLOCK_RESET                (HELPID_GAUGE + 0x00A4)
#define HELPID_GAUGE_CLOSE_POPUP_WINDOW         (HELPID_GAUGE + 0x00A5)


#define TOOLTIPTEXT_BASE                            0x3000

#define TOOLTIPTEXT_AIRSPEED_KILOS                  (TOOLTIPTEXT_BASE + 0x0001)
#define TOOLTIPTEXT_AIRSPEED_KNOTS                  (TOOLTIPTEXT_BASE + 0x0002)
#define TOOLTIPTEXT_AIRSPEED_MILES                  (TOOLTIPTEXT_BASE + 0x0003)
#define TOOLTIPTEXT_RADIO_ALTIMETER_METERS          (TOOLTIPTEXT_BASE + 0x0008)
#define TOOLTIPTEXT_RADIO_ALTIMETER_FEET            (TOOLTIPTEXT_BASE + 0x0009)
#define TOOLTIPTEXT_RADIO_ALTIMETER_DH_METERS       (TOOLTIPTEXT_BASE + 0x000A)
#define TOOLTIPTEXT_RADIO_ALTIMETER_DH_FEET         (TOOLTIPTEXT_BASE + 0x000B)
#define TOOLTIPTEXT_RADIO_ALTIMETER_DH_LIGHT        (TOOLTIPTEXT_BASE + 0x000C)
#define TOOLTIPTEXT_FLAP_INDICATOR_POSITION         (TOOLTIPTEXT_BASE + 0x0010)
#define TOOLTIPTEXT_FLAP_LEVER_DEGREES              (TOOLTIPTEXT_BASE + 0x0011)
#define TOOLTIPTEXT_FLAP_INDICATOR_DEGREES          (TOOLTIPTEXT_BASE + 0x0012)
#define TOOLTIPTEXT_FLAPS_SWITCH                    (TOOLTIPTEXT_BASE + 0x0013)
#define TOOLTIPTEXT_CLOCK                           (TOOLTIPTEXT_BASE + 0x0018)
#define TOOLTIPTEXT_CLOCK_SIMRATE                   (TOOLTIPTEXT_BASE + 0x0019)
#define TOOLTIPTEXT_WATER_RUDDER_HANDLE             (TOOLTIPTEXT_BASE + 0x0020)
#define TOOLTIPTEXT_LIGHT_ENGINE_FIRE               (TOOLTIPTEXT_BASE + 0x0030)
#define TOOLTIPTEXT_LIGHT_OIL_PRESSURE_LOW          (TOOLTIPTEXT_BASE + 0x0031)
#define TOOLTIPTEXT_LIGHT_GENERATOR_OFF             (TOOLTIPTEXT_BASE + 0x0032)
#define TOOLTIPTEXT_LIGHT_EMERGENCY_POWER_LEVER_ON  (TOOLTIPTEXT_BASE + 0x0033)
#define TOOLTIPTEXT_LIGHT_AUXILIARY_FUEL_PUMP_ON    (TOOLTIPTEXT_BASE + 0x0034)
#define TOOLTIPTEXT_LIGHT_FUEL_PRESSURE_LOW         (TOOLTIPTEXT_BASE + 0x0035)
#define TOOLTIPTEXT_LIGHT_STARTER_ON                (TOOLTIPTEXT_BASE + 0x0036)
#define TOOLTIPTEXT_LIGHT_IGNITION_ON               (TOOLTIPTEXT_BASE + 0x0037)
#define TOOLTIPTEXT_LIGHT_VOLTAGE_LOW               (TOOLTIPTEXT_BASE + 0x0038)
#define TOOLTIPTEXT_LIGHT_VACUUM_LOW                (TOOLTIPTEXT_BASE + 0x0039)
#define TOOLTIPTEXT_LIGHT_RESERVOIR_FUEL_LOW        (TOOLTIPTEXT_BASE + 0x003A)
#define TOOLTIPTEXT_LIGHT_LEFT_TANK_FUEL_LOW        (TOOLTIPTEXT_BASE + 0x003B)
#define TOOLTIPTEXT_LIGHT_RIGHT_TANK_FUEL_LOW       (TOOLTIPTEXT_BASE + 0x003C)
#define TOOLTIPTEXT_LIGHT_STANDBY_POWER_ON          (TOOLTIPTEXT_BASE + 0x003D)
#define TOOLTIPTEXT_LIGHT_DEICE_ON                  (TOOLTIPTEXT_BASE + 0x003E)
#define TOOLTIPTEXT_LIGHT_FUEL_SELECTOR_NOT_ALL     (TOOLTIPTEXT_BASE + 0x003F)
#define TOOLTIPTEXT_ALTIMETER_METERS                (TOOLTIPTEXT_BASE + 0x0050)
#define TOOLTIPTEXT_ALTIMETER_FEET                  (TOOLTIPTEXT_BASE + 0x0051)
#define TOOLTIPTEXT_ALTIMETER_FEET_METERS_SPECIAL   (TOOLTIPTEXT_BASE + 0x0052)
#define TOOLTIPTEXT_ALTIMETER_KOHLSMAN_INHG         (TOOLTIPTEXT_BASE + 0x0053)
#define TOOLTIPTEXT_ALTIMETER_KOHLSMAN_MBAR         (TOOLTIPTEXT_BASE + 0x0054)
#define TOOLTIPTEXT_AUTOPILOT_MASTER                (TOOLTIPTEXT_BASE + 0x0060)
#define TOOLTIPTEXT_AUTOPILOT_FLIGHT_DIRECTOR_SWITCH (TOOLTIPTEXT_BASE + 0x0061)
#define TOOLTIPTEXT_AUTOPILOT_WING_LEVELER          (TOOLTIPTEXT_BASE + 0x0062)
#define TOOLTIPTEXT_AUTOPILOT_IAS_HOLD              (TOOLTIPTEXT_BASE + 0x0063)
#define TOOLTIPTEXT_AUTOPILOT_BC_HOLD               (TOOLTIPTEXT_BASE + 0x0064)
#define TOOLTIPTEXT_AUTOPILOT_APP_HOLD              (TOOLTIPTEXT_BASE + 0x0065)
#define TOOLTIPTEXT_AUTOPILOT_YAW_DAMP_HOLD         (TOOLTIPTEXT_BASE + 0x0066)
#define TOOLTIPTEXT_AUTOPILOT_NAV_HOLD              (TOOLTIPTEXT_BASE + 0x0067)
#define TOOLTIPTEXT_AUTOPILOT_HEADING_HOLD          (TOOLTIPTEXT_BASE + 0x0068)
#define TOOLTIPTEXT_AUTOPILOT_HEADING_INDICATOR     (TOOLTIPTEXT_BASE + 0x0069)
#define TOOLTIPTEXT_AUTOPILOT_VSI_HOLD              (TOOLTIPTEXT_BASE + 0x006A)
#define TOOLTIPTEXT_AUTOPILOT_VSI_INDICATOR_FTMIN   (TOOLTIPTEXT_BASE + 0x006B)
#define TOOLTIPTEXT_AUTOPILOT_VSI_INDICATOR_MS      (TOOLTIPTEXT_BASE + 0x006C)
#define TOOLTIPTEXT_AUTOPILOT_ALTITUDE_HOLD         (TOOLTIPTEXT_BASE + 0x006D)
#define TOOLTIPTEXT_AUTOPILOT_ALTITUDE_INDICATOR_FEET (TOOLTIPTEXT_BASE + 0x006E)
#define TOOLTIPTEXT_AUTOPILOT_ALTITUDE_INDICATOR_METERS (TOOLTIPTEXT_BASE + 0x006F)
#define TOOLTIPTEXT_AUTOPILOT_SPEED_HOLD            (TOOLTIPTEXT_BASE + 0x0070)
#define TOOLTIPTEXT_AUTOPILOT_SPEED_INDICATOR_KNOTS (TOOLTIPTEXT_BASE + 0x0071)
#define TOOLTIPTEXT_AUTOPILOT_SPEED_INDICATOR_KILOS (TOOLTIPTEXT_BASE + 0x0072)
#define TOOLTIPTEXT_AUTOPILOT_AUTOTHROTTLE          (TOOLTIPTEXT_BASE + 0x0073)
#define TOOLTIPTEXT_AUTOPILOT_MACH_HOLD             (TOOLTIPTEXT_BASE + 0x0074)
#define TOOLTIPTEXT_AUTOPILOT_MACH_INDICATOR        (TOOLTIPTEXT_BASE + 0x0075)
#define TOOLTIPTEXT_SUCTION_MBAR                    (TOOLTIPTEXT_BASE + 0x0080)
#define TOOLTIPTEXT_SUCTION_INHG                    (TOOLTIPTEXT_BASE + 0x0081)
#define TOOLTIPTEXT_TURN_COORDINATOR_RATE           (TOOLTIPTEXT_BASE + 0x0090)
#define TOOLTIPTEXT_ATTITUDE_INDICATOR_BANK_PITCH   (TOOLTIPTEXT_BASE + 0x0092)
#define TOOLTIPTEXT_HSI_HEADING_COURSE              (TOOLTIPTEXT_BASE + 0x0094)
#define TOOLTIPTEXT_VSI_METERS_PER_SEC              (TOOLTIPTEXT_BASE + 0x0098)
#define TOOLTIPTEXT_VSI_FEET_PER_MIN                (TOOLTIPTEXT_BASE + 0x0099)
#define TOOLTIPTEXT_MANIFOLD_PRESSURE_INHG          (TOOLTIPTEXT_BASE + 0x00A0)
#define TOOLTIPTEXT_ENG1_MANIFOLD_PRESSURE_INHG     (TOOLTIPTEXT_BASE + 0x00A1)
#define TOOLTIPTEXT_ENG2_MANIFOLD_PRESSURE_INHG     (TOOLTIPTEXT_BASE + 0x00A2)
#define TOOLTIPTEXT_ENG3_MANIFOLD_PRESSURE_INHG     (TOOLTIPTEXT_BASE + 0x00A3)
#define TOOLTIPTEXT_ENG4_MANIFOLD_PRESSURE_INHG     (TOOLTIPTEXT_BASE + 0x00A4)
#define TOOLTIPTEXT_MANIFOLD_PRESSURE_PASCAL        (TOOLTIPTEXT_BASE + 0x00A8)
#define TOOLTIPTEXT_ENG1_MANIFOLD_PRESSURE_PASCAL   (TOOLTIPTEXT_BASE + 0x00A9)
#define TOOLTIPTEXT_ENG2_MANIFOLD_PRESSURE_PASCAL   (TOOLTIPTEXT_BASE + 0x00AA)
#define TOOLTIPTEXT_ENG3_MANIFOLD_PRESSURE_PASCAL   (TOOLTIPTEXT_BASE + 0x00AB)
#define TOOLTIPTEXT_ENG4_MANIFOLD_PRESSURE_PASCAL   (TOOLTIPTEXT_BASE + 0x00AC)
#define TOOLTIPTEXT_PROP_RPM                        (TOOLTIPTEXT_BASE + 0x00B0)
#define TOOLTIPTEXT_ENG1_PROP_RPM                   (TOOLTIPTEXT_BASE + 0x00B1)
#define TOOLTIPTEXT_ENG2_PROP_RPM                   (TOOLTIPTEXT_BASE + 0x00B2)
#define TOOLTIPTEXT_ENG3_PROP_RPM                   (TOOLTIPTEXT_BASE + 0x00B3)
#define TOOLTIPTEXT_ENG4_PROP_RPM                   (TOOLTIPTEXT_BASE + 0x00B4)
#define TOOLTIPTEXT_PROP_SYNCHROSCOPE               (TOOLTIPTEXT_BASE + 0x00B8)
#define TOOLTIPTEXT_FUEL_FLOW_GALLON_HOUR           (TOOLTIPTEXT_BASE + 0x00C0)
#define TOOLTIPTEXT_ENG1_FUEL_FLOW_GALLON_HOUR      (TOOLTIPTEXT_BASE + 0x00C1)
#define TOOLTIPTEXT_ENG2_FUEL_FLOW_GALLON_HOUR      (TOOLTIPTEXT_BASE + 0x00C2)
#define TOOLTIPTEXT_ENG3_FUEL_FLOW_GALLON_HOUR      (TOOLTIPTEXT_BASE + 0x00C3)
#define TOOLTIPTEXT_ENG4_FUEL_FLOW_GALLON_HOUR      (TOOLTIPTEXT_BASE + 0x00C4)
#define TOOLTIPTEXT_FUEL_FLOW_LITER_HOUR            (TOOLTIPTEXT_BASE + 0x00C5)
#define TOOLTIPTEXT_ENG1_FUEL_FLOW_LITER_HOUR       (TOOLTIPTEXT_BASE + 0x00C6)
#define TOOLTIPTEXT_ENG2_FUEL_FLOW_LITER_HOUR       (TOOLTIPTEXT_BASE + 0x00C7)
#define TOOLTIPTEXT_ENG3_FUEL_FLOW_LITER_HOUR       (TOOLTIPTEXT_BASE + 0x00C8)
#define TOOLTIPTEXT_ENG4_FUEL_FLOW_LITER_HOUR       (TOOLTIPTEXT_BASE + 0x00C9)
#define TOOLTIPTEXT_FUEL_FLOW_POUND_HOUR            (TOOLTIPTEXT_BASE + 0x00CA)
#define TOOLTIPTEXT_ENG1_FUEL_FLOW_POUND_HOUR       (TOOLTIPTEXT_BASE + 0x00CB)
#define TOOLTIPTEXT_ENG2_FUEL_FLOW_POUND_HOUR       (TOOLTIPTEXT_BASE + 0x00CC)
#define TOOLTIPTEXT_ENG3_FUEL_FLOW_POUND_HOUR       (TOOLTIPTEXT_BASE + 0x00CD)
#define TOOLTIPTEXT_ENG4_FUEL_FLOW_POUND_HOUR       (TOOLTIPTEXT_BASE + 0x00CE)
#define TOOLTIPTEXT_CHT_CELSIUS                     (TOOLTIPTEXT_BASE + 0x00D0)
#define TOOLTIPTEXT_ENG1_CHT_CELSIUS                (TOOLTIPTEXT_BASE + 0x00D1)
#define TOOLTIPTEXT_ENG2_CHT_CELSIUS                (TOOLTIPTEXT_BASE + 0x00D2)
#define TOOLTIPTEXT_ENG3_CHT_CELSIUS                (TOOLTIPTEXT_BASE + 0x00D3)
#define TOOLTIPTEXT_ENG4_CHT_CELSIUS                (TOOLTIPTEXT_BASE + 0x00D4)
#define TOOLTIPTEXT_CHT_FARENHEIT                   (TOOLTIPTEXT_BASE + 0x00D8)
#define TOOLTIPTEXT_ENG1_CHT_FARENHEIT              (TOOLTIPTEXT_BASE + 0x00D9)
#define TOOLTIPTEXT_ENG2_CHT_FARENHEIT              (TOOLTIPTEXT_BASE + 0x00DA)
#define TOOLTIPTEXT_ENG3_CHT_FARENHEIT              (TOOLTIPTEXT_BASE + 0x00DB)
#define TOOLTIPTEXT_ENG4_CHT_FARENHEIT              (TOOLTIPTEXT_BASE + 0x00DC)
#define TOOLTIPTEXT_EGT_CELSIUS                     (TOOLTIPTEXT_BASE + 0x00E0)
#define TOOLTIPTEXT_ENG1_EGT_CELSIUS                (TOOLTIPTEXT_BASE + 0x00E1)
#define TOOLTIPTEXT_ENG2_EGT_CELSIUS                (TOOLTIPTEXT_BASE + 0x00E2)
#define TOOLTIPTEXT_ENG3_EGT_CELSIUS                (TOOLTIPTEXT_BASE + 0x00E3)
#define TOOLTIPTEXT_ENG4_EGT_CELSIUS                (TOOLTIPTEXT_BASE + 0x00E4)
#define TOOLTIPTEXT_EGT_FARENHEIT                   (TOOLTIPTEXT_BASE + 0x00E8)
#define TOOLTIPTEXT_ENG1_EGT_FARENHEIT              (TOOLTIPTEXT_BASE + 0x00E9)
#define TOOLTIPTEXT_ENG2_EGT_FARENHEIT              (TOOLTIPTEXT_BASE + 0x00EA)
#define TOOLTIPTEXT_ENG3_EGT_FARENHEIT              (TOOLTIPTEXT_BASE + 0x00EB)
#define TOOLTIPTEXT_ENG4_EGT_FARENHEIT              (TOOLTIPTEXT_BASE + 0x00EC)
#define TOOLTIPTEXT_OIL_TEMPERATURE_CELSIUS         (TOOLTIPTEXT_BASE + 0x00F0)
#define TOOLTIPTEXT_ENG1_OIL_TEMPERATURE_CELSIUS    (TOOLTIPTEXT_BASE + 0x00F1) 
#define TOOLTIPTEXT_ENG2_OIL_TEMPERATURE_CELSIUS    (TOOLTIPTEXT_BASE + 0x00F2) 
#define TOOLTIPTEXT_ENG3_OIL_TEMPERATURE_CELSIUS    (TOOLTIPTEXT_BASE + 0x00F3) 
#define TOOLTIPTEXT_ENG4_OIL_TEMPERATURE_CELSIUS    (TOOLTIPTEXT_BASE + 0x00F4) 
#define TOOLTIPTEXT_OIL_TEMPERATURE_FARENHEIT       (TOOLTIPTEXT_BASE + 0x00F8)
#define TOOLTIPTEXT_ENG1_OIL_TEMPERATURE_FARENHEIT  (TOOLTIPTEXT_BASE + 0x00F9)
#define TOOLTIPTEXT_ENG2_OIL_TEMPERATURE_FARENHEIT  (TOOLTIPTEXT_BASE + 0x00FA)  
#define TOOLTIPTEXT_ENG3_OIL_TEMPERATURE_FARENHEIT  (TOOLTIPTEXT_BASE + 0x00FB)  
#define TOOLTIPTEXT_ENG4_OIL_TEMPERATURE_FARENHEIT  (TOOLTIPTEXT_BASE + 0x00FC)  
#define TOOLTIPTEXT_OIL_PRESSURE_PSI                (TOOLTIPTEXT_BASE + 0x0100)
#define TOOLTIPTEXT_ENG1_OIL_PRESSURE_PSI           (TOOLTIPTEXT_BASE + 0x0101)
#define TOOLTIPTEXT_ENG2_OIL_PRESSURE_PSI           (TOOLTIPTEXT_BASE + 0x0102)
#define TOOLTIPTEXT_ENG3_OIL_PRESSURE_PSI           (TOOLTIPTEXT_BASE + 0x0103)
#define TOOLTIPTEXT_ENG4_OIL_PRESSURE_PSI           (TOOLTIPTEXT_BASE + 0x0104)
#define TOOLTIPTEXT_OIL_PRESSURE_KGFSQCM            (TOOLTIPTEXT_BASE + 0x0108)
#define TOOLTIPTEXT_ENG1_OIL_PRESSURE_KGFSQCM       (TOOLTIPTEXT_BASE + 0x0109)
#define TOOLTIPTEXT_ENG2_OIL_PRESSURE_KGFSQCM       (TOOLTIPTEXT_BASE + 0x010A)
#define TOOLTIPTEXT_ENG3_OIL_PRESSURE_KGFSQCM       (TOOLTIPTEXT_BASE + 0x010B)
#define TOOLTIPTEXT_ENG4_OIL_PRESSURE_KGFSQCM       (TOOLTIPTEXT_BASE + 0x010C)
#define TOOLTIPTEXT_LEFT_FUEL_PERCENT               (TOOLTIPTEXT_BASE + 0x0110)
#define TOOLTIPTEXT_RIGHT_FUEL_PERCENT              (TOOLTIPTEXT_BASE + 0x0111)
#define TOOLTIPTEXT_LEFT_FUEL_LBS                   (TOOLTIPTEXT_BASE + 0x0112)
#define TOOLTIPTEXT_RIGHT_FUEL_LBS                  (TOOLTIPTEXT_BASE + 0x0113)
#define TOOLTIPTEXT_LEFT_FUEL_KGS                   (TOOLTIPTEXT_BASE + 0x0114)
#define TOOLTIPTEXT_RIGHT_FUEL_KGS                  (TOOLTIPTEXT_BASE + 0x0115)
#define TOOLTIPTEXT_LEFT_FUEL_GALLONS               (TOOLTIPTEXT_BASE + 0x0116)
#define TOOLTIPTEXT_RIGHT_FUEL_GALLONS              (TOOLTIPTEXT_BASE + 0x0117)
#define TOOLTIPTEXT_LEFT_FUEL_LITERS                (TOOLTIPTEXT_BASE + 0x0118)
#define TOOLTIPTEXT_RIGHT_FUEL_LITERS               (TOOLTIPTEXT_BASE + 0x0119)
#define TOOLTIPTEXT_MARKER_OUTER                    (TOOLTIPTEXT_BASE + 0x0120)
#define TOOLTIPTEXT_MARKER_MIDDLE                   (TOOLTIPTEXT_BASE + 0x0121)
#define TOOLTIPTEXT_MARKER_INNER                    (TOOLTIPTEXT_BASE + 0x0122)
#define TOOLTIPTEXT_PITCH_TRIM                      (TOOLTIPTEXT_BASE + 0x0128)
#define TOOLTIPTEXT_ROLL_TRIM                       (TOOLTIPTEXT_BASE + 0x0129)
#define TOOLTIPTEXT_YAW_TRIM                        (TOOLTIPTEXT_BASE + 0x012A)
#define TOOLTIPTEXT_YAW_TRIM_INDICATOR              (TOOLTIPTEXT_BASE + 0x012B)
#define TOOLTIPTEXT_YAW_TRIM_KNOB                   (TOOLTIPTEXT_BASE + 0x012C)
#define TOOLTIPTEXT_GEAR_LIGHT                      (TOOLTIPTEXT_BASE + 0x0130)
#define TOOLTIPTEXT_GEAR_LEVER                      (TOOLTIPTEXT_BASE + 0x0131)
#define TOOLTIPTEXT_AMMETER                         (TOOLTIPTEXT_BASE + 0x0140)
#define TOOLTIPTEXT_AUDIO_PANEL_TRANSMIT_RECEIVE    (TOOLTIPTEXT_BASE + 0x0150)
#define TOOLTIPTEXT_COMM1_FREQ_ACTIVE_STANDBY       (TOOLTIPTEXT_BASE + 0x0160)
#define TOOLTIPTEXT_COMM2_FREQ_ACTIVE_STANDBY       (TOOLTIPTEXT_BASE + 0x0161)
#define TOOLTIPTEXT_NAV1_FREQ_ACTIVE_STANDBY        (TOOLTIPTEXT_BASE + 0x0168)
#define TOOLTIPTEXT_NAV2_FREQ_ACTIVE_STANDBY        (TOOLTIPTEXT_BASE + 0x0169)
#define TOOLTIPTEXT_ADF_FREQ                        (TOOLTIPTEXT_BASE + 0x0170)
#define TOOLTIPTEXT_DME_NAV_NM_KT                   (TOOLTIPTEXT_BASE + 0x0178)
#define TOOLTIPTEXT_DME_NAV_KM_MS                   (TOOLTIPTEXT_BASE + 0x0179)
#define TOOLTIPTEXT_DME_NAV_NM_KT_MIN               (TOOLTIPTEXT_BASE + 0x017A)
#define TOOLTIPTEXT_DME_NAV_KM_MS_MIN               (TOOLTIPTEXT_BASE + 0x017B)
#define TOOLTIPTEXT_DME_N1_N2_SELECT                (TOOLTIPTEXT_BASE + 0x017C)
#define TOOLTIPTEXT_TRANSPONDER_ID                  (TOOLTIPTEXT_BASE + 0x0180)
#define TOOLTIPTEXT_THROTTLE_MIXTURE_PERCENT        (TOOLTIPTEXT_BASE + 0x0190)
#define TOOLTIPTEXT_THROTTLE_ENG1_MIXTURE_PERCENT   (TOOLTIPTEXT_BASE + 0x0191)
#define TOOLTIPTEXT_THROTTLE_ENG2_MIXTURE_PERCENT   (TOOLTIPTEXT_BASE + 0x0192)
#define TOOLTIPTEXT_THROTTLE_ENG3_MIXTURE_PERCENT   (TOOLTIPTEXT_BASE + 0x0193)
#define TOOLTIPTEXT_THROTTLE_ENG4_MIXTURE_PERCENT   (TOOLTIPTEXT_BASE + 0x0194)
#define TOOLTIPTEXT_THROTTLE_PROPELLER_PERCENT      (TOOLTIPTEXT_BASE + 0x0198)
#define TOOLTIPTEXT_THROTTLE_ENG1_PROPELLER_PERCENT (TOOLTIPTEXT_BASE + 0x0199)
#define TOOLTIPTEXT_THROTTLE_ENG2_PROPELLER_PERCENT (TOOLTIPTEXT_BASE + 0x019A)
#define TOOLTIPTEXT_THROTTLE_ENG3_PROPELLER_PERCENT (TOOLTIPTEXT_BASE + 0x019B)
#define TOOLTIPTEXT_THROTTLE_ENG4_PROPELLER_PERCENT (TOOLTIPTEXT_BASE + 0x019C)
#define TOOLTIPTEXT_THROTTLE_THROTTLE_PERCENT       (TOOLTIPTEXT_BASE + 0x01A0)
#define TOOLTIPTEXT_THROTTLE_ENG1_THROTTLE_PERCENT  (TOOLTIPTEXT_BASE + 0x01A1)
#define TOOLTIPTEXT_THROTTLE_ENG2_THROTTLE_PERCENT  (TOOLTIPTEXT_BASE + 0x01A2)
#define TOOLTIPTEXT_THROTTLE_ENG3_THROTTLE_PERCENT  (TOOLTIPTEXT_BASE + 0x01A3)
#define TOOLTIPTEXT_THROTTLE_ENG4_THROTTLE_PERCENT  (TOOLTIPTEXT_BASE + 0x01A4)
#define TOOLTIPTEXT_COWL_FLAPS_PERCENT              (TOOLTIPTEXT_BASE + 0x01A8)
#define TOOLTIPTEXT_ENG1_COWL_FLAPS_PERCENT         (TOOLTIPTEXT_BASE + 0x01A9)
#define TOOLTIPTEXT_ENG2_COWL_FLAPS_PERCENT         (TOOLTIPTEXT_BASE + 0x01AA)
#define TOOLTIPTEXT_ENG3_COWL_FLAPS_PERCENT         (TOOLTIPTEXT_BASE + 0x01AB)
#define TOOLTIPTEXT_ENG4_COWL_FLAPS_PERCENT         (TOOLTIPTEXT_BASE + 0x01AC)
#define TOOLTIPTEXT_FUEL_SELECTOR                   (TOOLTIPTEXT_BASE + 0x01B0)
#define TOOLTIPTEXT_LEFT_FUEL_SELECTOR              (TOOLTIPTEXT_BASE + 0x01B1)
#define TOOLTIPTEXT_RIGHT_FUEL_SELECTOR             (TOOLTIPTEXT_BASE + 0x01B2)
#define TOOLTIPTEXT_WHISKEY_COMPASS                 (TOOLTIPTEXT_BASE + 0x01B8)
#define TOOLTIPTEXT_DEICE_SWITCH                    (TOOLTIPTEXT_BASE + 0x01BC)
#define TOOLTIPTEXT_PITOT_HEAT_SWITCH               (TOOLTIPTEXT_BASE + 0x01BD)
#define TOOLTIPTEXT_TORQUE_PERCENT                  (TOOLTIPTEXT_BASE + 0x01C0)
#define TOOLTIPTEXT_ENG1_TORQUE_PERCENT             (TOOLTIPTEXT_BASE + 0x01C1)
#define TOOLTIPTEXT_ENG2_TORQUE_PERCENT             (TOOLTIPTEXT_BASE + 0x01C2)
#define TOOLTIPTEXT_ENG3_TORQUE_PERCENT             (TOOLTIPTEXT_BASE + 0x01C3)
#define TOOLTIPTEXT_ENG4_TORQUE_PERCENT             (TOOLTIPTEXT_BASE + 0x01C4)
#define TOOLTIPTEXT_TORQUE_FT_LBS                   (TOOLTIPTEXT_BASE + 0x01C5)
#define TOOLTIPTEXT_ENG1_TORQUE_FT_LBS              (TOOLTIPTEXT_BASE + 0x01C6)
#define TOOLTIPTEXT_ENG2_TORQUE_FT_LBS              (TOOLTIPTEXT_BASE + 0x01C7)
#define TOOLTIPTEXT_ENG3_TORQUE_FT_LBS              (TOOLTIPTEXT_BASE + 0x01C8)
#define TOOLTIPTEXT_ENG4_TORQUE_FT_LBS              (TOOLTIPTEXT_BASE + 0x01C9)
#define TOOLTIPTEXT_TORQUE_NEWTON                   (TOOLTIPTEXT_BASE + 0x01CA)
#define TOOLTIPTEXT_ENG1_TORQUE_NEWTON              (TOOLTIPTEXT_BASE + 0x01CB)
#define TOOLTIPTEXT_ENG2_TORQUE_NEWTON              (TOOLTIPTEXT_BASE + 0x01CC)
#define TOOLTIPTEXT_ENG3_TORQUE_NEWTON              (TOOLTIPTEXT_BASE + 0x01CD)
#define TOOLTIPTEXT_ENG4_TORQUE_NEWTON              (TOOLTIPTEXT_BASE + 0x01CE)
#define TOOLTIPTEXT_ITT_RANKINE                     (TOOLTIPTEXT_BASE + 0x01D0)
#define TOOLTIPTEXT_ENG1_ITT_RANKINE                (TOOLTIPTEXT_BASE + 0x01D1)
#define TOOLTIPTEXT_ENG2_ITT_RANKINE                (TOOLTIPTEXT_BASE + 0x01D2)
#define TOOLTIPTEXT_ENG3_ITT_RANKINE                (TOOLTIPTEXT_BASE + 0x01D3)
#define TOOLTIPTEXT_ENG4_ITT_RANKINE                (TOOLTIPTEXT_BASE + 0x01D4)
#define TOOLTIPTEXT_ITT_CELSIUS                     (TOOLTIPTEXT_BASE + 0x01D5)
#define TOOLTIPTEXT_ENG1_ITT_CELSIUS                (TOOLTIPTEXT_BASE + 0x01D6)
#define TOOLTIPTEXT_ENG2_ITT_CELSIUS                (TOOLTIPTEXT_BASE + 0x01D7)
#define TOOLTIPTEXT_ENG3_ITT_CELSIUS                (TOOLTIPTEXT_BASE + 0x01D8)
#define TOOLTIPTEXT_ENG4_ITT_CELSIUS                (TOOLTIPTEXT_BASE + 0x01D9)
#define TOOLTIPTEXT_GAS_GENERATOR_RPM_PERCENT       (TOOLTIPTEXT_BASE + 0x01E0)
#define TOOLTIPTEXT_ENG1_GAS_GENERATOR_RPM_PERCENT  (TOOLTIPTEXT_BASE + 0x01E1)
#define TOOLTIPTEXT_ENG2_GAS_GENERATOR_RPM_PERCENT  (TOOLTIPTEXT_BASE + 0x01E2)
#define TOOLTIPTEXT_ENG3_GAS_GENERATOR_RPM_PERCENT  (TOOLTIPTEXT_BASE + 0x01E3)
#define TOOLTIPTEXT_ENG4_GAS_GENERATOR_RPM_PERCENT  (TOOLTIPTEXT_BASE + 0x01E4)
#define TOOLTIPTEXT_THROTTLE_FUEL_CONDITION_PERCENT (TOOLTIPTEXT_BASE + 0x01E8)
#define TOOLTIPTEXT_ENG1_THROTTLE_FUEL_CONDITION_PERCENT (TOOLTIPTEXT_BASE + 0x01E9)
#define TOOLTIPTEXT_ENG2_THROTTLE_FUEL_CONDITION_PERCENT (TOOLTIPTEXT_BASE + 0x01EA)
#define TOOLTIPTEXT_ENG3_THROTTLE_FUEL_CONDITION_PERCENT (TOOLTIPTEXT_BASE + 0x01EB)
#define TOOLTIPTEXT_ENG4_THROTTLE_FUEL_CONDITION_PERCENT (TOOLTIPTEXT_BASE + 0x01EC)
#define TOOLTIPTEXT_PARKING_BRAKE                   (TOOLTIPTEXT_BASE + 0x01F0)
#define TOOLTIPTEXT_FUEL_CUTOFF                     (TOOLTIPTEXT_BASE + 0x01F8)
#define TOOLTIPTEXT_ENG1_FUEL_CUTOFF                (TOOLTIPTEXT_BASE + 0x01F9)
#define TOOLTIPTEXT_ENG2_FUEL_CUTOFF                (TOOLTIPTEXT_BASE + 0x01FA)
#define TOOLTIPTEXT_ENG3_FUEL_CUTOFF                (TOOLTIPTEXT_BASE + 0x01FB)
#define TOOLTIPTEXT_ENG4_FUEL_CUTOFF                (TOOLTIPTEXT_BASE + 0x01FC)
#define TOOLTIPTEXT_SPEED_BRAKE                     (TOOLTIPTEXT_BASE + 0x0200)
#define TOOLTIPTEXT_HEADING_INDICATOR_HEADING       (TOOLTIPTEXT_BASE + 0x0208)
#define TOOLTIPTEXT_TACHOMETER_RPM                  (TOOLTIPTEXT_BASE + 0x0210)
#define TOOLTIPTEXT_ENG1_TACHOMETER_RPM             (TOOLTIPTEXT_BASE + 0x0211)
#define TOOLTIPTEXT_ENG2_TACHOMETER_RPM             (TOOLTIPTEXT_BASE + 0x0212)
#define TOOLTIPTEXT_ENG3_TACHOMETER_RPM             (TOOLTIPTEXT_BASE + 0x0213)
#define TOOLTIPTEXT_ENG4_TACHOMETER_RPM             (TOOLTIPTEXT_BASE + 0x0214)
#define TOOLTIPTEXT_ON                              (TOOLTIPTEXT_BASE + 0x0220)
#define TOOLTIPTEXT_OFF                             (TOOLTIPTEXT_BASE + 0x0221)
#define TOOLTIPTEXT_AMBIENT_TEMPERATURE_CELSIUS     (TOOLTIPTEXT_BASE + 0x0224)
#define TOOLTIPTEXT_AMBIENT_TEMPERATURE_FARENHEIT   (TOOLTIPTEXT_BASE + 0x0225)
#define TOOLTIPTEXT_ENG1_CARB_TEMPERATURE_CELSIUS   (TOOLTIPTEXT_BASE + 0x0228)
#define TOOLTIPTEXT_ENG2_CARB_TEMPERATURE_CELSIUS   (TOOLTIPTEXT_BASE + 0x0229)
#define TOOLTIPTEXT_CABIN_DOORS                     (TOOLTIPTEXT_BASE + 0x0230)
#define TOOLTIPTEXT_DC3_FUEL_GAUGE                  (TOOLTIPTEXT_BASE + 0x0231)
#define TOOLTIPTEXT_PROP_FEATHER_SWITCH             (TOOLTIPTEXT_BASE + 0x0232)
#define TOOLTIPTEXT_PROP_FEATHER_SWITCH_1           (TOOLTIPTEXT_BASE + 0x0233)
#define TOOLTIPTEXT_PROP_FEATHER_SWITCH_2           (TOOLTIPTEXT_BASE + 0x0234)
#define TOOLTIPTEXT_PROP_FEATHER_SWITCH_3           (TOOLTIPTEXT_BASE + 0x0235)
#define TOOLTIPTEXT_PROP_FEATHER_SWITCH_4           (TOOLTIPTEXT_BASE + 0x0236)
#define TOOLTIPTEXT_MASTER_IGNITION_SWITCH          (TOOLTIPTEXT_BASE + 0x0238)
#define TOOLTIPTEXT_HYDRAULIC_PRESSURE              (TOOLTIPTEXT_BASE + 0x0240)
#define TOOLTIPTEXT_GEAR_HYDRAULIC_PRESSURE         (TOOLTIPTEXT_BASE + 0x0241)
#define TOOLTIPTEXT_COMM1_FREQ                      (TOOLTIPTEXT_BASE + 0x0242)
#define TOOLTIPTEXT_COMM2_FREQ                      (TOOLTIPTEXT_BASE + 0x0243)
#define TOOLTIPTEXT_NAV1_FREQ                       (TOOLTIPTEXT_BASE + 0x0244)
#define TOOLTIPTEXT_NAV2_FREQ                       (TOOLTIPTEXT_BASE + 0x0245)
#define TOOLTIPTEXT_ADF1_FREQ                       (TOOLTIPTEXT_BASE + 0x0246)
#define TOOLTIPTEXT_ADF2_FREQ                       (TOOLTIPTEXT_BASE + 0x0247)
#define TOOLTIPTEXT_ADF1_BAND_SELECTOR              (TOOLTIPTEXT_BASE + 0x0248)
#define TOOLTIPTEXT_ADF2_BAND_SELECTOR              (TOOLTIPTEXT_BASE + 0x0249)
#define TOOLTIPTEXT_ADF1_AUDIO_SELECTOR             (TOOLTIPTEXT_BASE + 0x024A)
#define TOOLTIPTEXT_ADF2_AUDIO_SELECTOR             (TOOLTIPTEXT_BASE + 0x024B)
#define TOOLTIPTEXT_NAV1_AUDIO_SELECTOR             (TOOLTIPTEXT_BASE + 0x024C)
#define TOOLTIPTEXT_NAV2_AUDIO_SELECTOR             (TOOLTIPTEXT_BASE + 0x024D)
#define TOOLTIPTEXT_COM1_AUDIO_SELECTOR             (TOOLTIPTEXT_BASE + 0x024E)
#define TOOLTIPTEXT_COM2_AUDIO_SELECTOR             (TOOLTIPTEXT_BASE + 0x024F)
#define TOOLTIPTEXT_MARKER_AUDIO_SELECTOR           (TOOLTIPTEXT_BASE + 0x0250)
#define TOOLTIPTEXT_ADF_SELECTOR                    (TOOLTIPTEXT_BASE + 0x0251)
#define TOOLTIPTEXT_NAV_SELECTOR                    (TOOLTIPTEXT_BASE + 0x0252)
#define TOOLTIPTEXT_DC3_FLAP_LEVER_EXTEND           (TOOLTIPTEXT_BASE + 0x0253)
#define TOOLTIPTEXT_DC3_FLAP_LEVER_RETRACT          (TOOLTIPTEXT_BASE + 0x0254)
#define TOOLTIPTEXT_TAILWHEEL_LOCK                  (TOOLTIPTEXT_BASE + 0x0255)
#define TOOLTIPTEXT_FORD_TRIMOTOR_FUEL_GAUGE        (TOOLTIPTEXT_BASE + 0x0260)
#define TOOLTIPTEXT_FORD_TRIMOTOR_AMMETER           (TOOLTIPTEXT_BASE + 0x0261)
#define TOOLTIPTEXT_AUTOPILOT_PITCH_REF             (TOOLTIPTEXT_BASE + 0x0264)
#define TOOLTIPTEXT_CENTER_FUEL_PERCENT             (TOOLTIPTEXT_BASE + 0x0265)
#define TOOLTIPTEXT_FUEL_SELECTOR_1                 (TOOLTIPTEXT_BASE + 0x0268)
#define TOOLTIPTEXT_FUEL_SELECTOR_2                 (TOOLTIPTEXT_BASE + 0x0269)
#define TOOLTIPTEXT_FUEL_SELECTOR_3                 (TOOLTIPTEXT_BASE + 0x026A)
#define TOOLTIPTEXT_FUEL_SELECTOR_4                 (TOOLTIPTEXT_BASE + 0x026B)
#define TOOLTIPTEXT_RADIATOR_TEMPERATURE_CELSIUS    (TOOLTIPTEXT_BASE + 0x0270)
#define TOOLTIPTEXT_RADIATOR_TEMPERATURE_FARENHEIT  (TOOLTIPTEXT_BASE + 0x0271)
#define TOOLTIPTEXT_PITCH_TRIM_PCT                  (TOOLTIPTEXT_BASE + 0x0278)
#define TOOLTIPTEXT_INCLINOMETER_BALL               (TOOLTIPTEXT_BASE + 0x0279)
#define TOOLTIPTEXT_MAGNETO_SWITCH                  (TOOLTIPTEXT_BASE + 0x0280)
#define TOOLTIPTEXT_STARTER_SWITCH                  (TOOLTIPTEXT_BASE + 0x0281)
#define TOOLTIPTEXT_TURN_INDICATOR_RATE             (TOOLTIPTEXT_BASE + 0x0282)
#define TOOLTIPTEXT_ENG1_CARB_HEAT                  (TOOLTIPTEXT_BASE + 0x0291)
#define TOOLTIPTEXT_ENG2_CARB_HEAT                  (TOOLTIPTEXT_BASE + 0x0292)
#define TOOLTIPTEXT_ENG3_CARB_HEAT                  (TOOLTIPTEXT_BASE + 0x0293)
#define TOOLTIPTEXT_ENG4_CARB_HEAT                  (TOOLTIPTEXT_BASE + 0x0294)
#define TOOLTIPTEXT_ENG1_MAGNETO_SWITCH             (TOOLTIPTEXT_BASE + 0x02A0)
#define TOOLTIPTEXT_ENG2_MAGNETO_SWITCH             (TOOLTIPTEXT_BASE + 0x02A1)
#define TOOLTIPTEXT_ENG3_MAGNETO_SWITCH             (TOOLTIPTEXT_BASE + 0x02A2)
#define TOOLTIPTEXT_ENG4_MAGNETO_SWITCH             (TOOLTIPTEXT_BASE + 0x02A3)
#define TOOLTIPTEXT_ENG1_PRIMER_SWITCH              (TOOLTIPTEXT_BASE + 0x02A8)
#define TOOLTIPTEXT_ENG2_PRIMER_SWITCH              (TOOLTIPTEXT_BASE + 0x02A9)
#define TOOLTIPTEXT_ENG3_PRIMER_SWITCH              (TOOLTIPTEXT_BASE + 0x02AA)
#define TOOLTIPTEXT_ENG4_PRIMER_SWITCH              (TOOLTIPTEXT_BASE + 0x02AB)
#define TOOLTIPTEXT_ENG1_STARTER_SWITCH             (TOOLTIPTEXT_BASE + 0x02B1)
#define TOOLTIPTEXT_ENG2_STARTER_SWITCH             (TOOLTIPTEXT_BASE + 0x02B2)
#define TOOLTIPTEXT_ENG3_STARTER_SWITCH             (TOOLTIPTEXT_BASE + 0x02B3)
#define TOOLTIPTEXT_ENG4_STARTER_SWITCH             (TOOLTIPTEXT_BASE + 0x02B4)
#define TOOLTIPTEXT_TOGGLE_ELECTRICAL_WINDOW        (TOOLTIPTEXT_BASE + 0x02C0)
#define TOOLTIPTEXT_TOGGLE_ENGINE_WINDOW            (TOOLTIPTEXT_BASE + 0x02C1)
#define TOOLTIPTEXT_TOGGLE_BACK_WINDOW              (TOOLTIPTEXT_BASE + 0x02C2)
#define TOOLTIPTEXT_TOGGLE_IGNITION_CONTROL_WINDOW  (TOOLTIPTEXT_BASE + 0x02C3)
#define TOOLTIPTEXT_TOGGLE_ENGINE_CONTROL_WINDOW    (TOOLTIPTEXT_BASE + 0x02C4)
#define TOOLTIPTEXT_TOGGLE_ENGINE_INSTRUMENTS_WINDOW (TOOLTIPTEXT_BASE + 0x02C5)
#define TOOLTIPTEXT_FORD_TRIMOTOR_PRIMER_SELECT     (TOOLTIPTEXT_BASE + 0x02C8)
#define TOOLTIPTEXT_FORD_TRIMOTOR_PRIMER_PULL       (TOOLTIPTEXT_BASE + 0x02C9)
#define TOOLTIPTEXT_TOGGLE_PIPER_CUB_STARTER_WINDOW (TOOLTIPTEXT_BASE + 0x02CC)
#define TOOLTIPTEXT_DC3_RMI_CARD                    (TOOLTIPTEXT_BASE + 0x02D0)
#define TOOLTIPTEXT_THROTTLE_ENG1_PROPELLER         (TOOLTIPTEXT_BASE + 0x02D8)
#define TOOLTIPTEXT_THROTTLE_ENG2_PROPELLER         (TOOLTIPTEXT_BASE + 0x02D9)
#define TOOLTIPTEXT_THROTTLE_ENG3_PROPELLER         (TOOLTIPTEXT_BASE + 0x02DA)
#define TOOLTIPTEXT_THROTTLE_ENG4_PROPELLER         (TOOLTIPTEXT_BASE + 0x02DB)
#define TOOLTIPTEXT_BATTERY_SWITCH                  (TOOLTIPTEXT_BASE + 0x02E0)
#define TOOLTIPTEXT_FUEL_PUMP                       (TOOLTIPTEXT_BASE + 0x02E8)
#define TOOLTIPTEXT_ENG1_FUEL_PUMP                  (TOOLTIPTEXT_BASE + 0x02E9)
#define TOOLTIPTEXT_ENG2_FUEL_PUMP                  (TOOLTIPTEXT_BASE + 0x02EA)
#define TOOLTIPTEXT_ENG3_FUEL_PUMP                  (TOOLTIPTEXT_BASE + 0x02EB)
#define TOOLTIPTEXT_ENG4_FUEL_PUMP                  (TOOLTIPTEXT_BASE + 0x02EC)
#define TOOLTIPTEXT_ENG1_GENERATOR_SWITCH           (TOOLTIPTEXT_BASE + 0x02F1)
#define TOOLTIPTEXT_ENG2_GENERATOR_SWITCH           (TOOLTIPTEXT_BASE + 0x02F2)
#define TOOLTIPTEXT_ENG3_GENERATOR_SWITCH           (TOOLTIPTEXT_BASE + 0x02F3)
#define TOOLTIPTEXT_ENG4_GENERATOR_SWITCH           (TOOLTIPTEXT_BASE + 0x02F4)
#define TOOLTIPTEXT_LIGHT_SWITCH_WING               (TOOLTIPTEXT_BASE + 0x0300)
#define TOOLTIPTEXT_LIGHT_SWITCH_NAV                (TOOLTIPTEXT_BASE + 0x0301)
#define TOOLTIPTEXT_LIGHT_SWITCH_LANDING            (TOOLTIPTEXT_BASE + 0x0302)
#define TOOLTIPTEXT_LIGHT_SWITCH_PANEL              (TOOLTIPTEXT_BASE + 0x0304)
#define TOOLTIPTEXT_LIGHT_SWITCH_STROBE             (TOOLTIPTEXT_BASE + 0x0305)
#define TOOLTIPTEXT_LIGHT_SWITCH_TAXI               (TOOLTIPTEXT_BASE + 0x0306)
#define TOOLTIPTEXT_LIGHT_SWITCH_BEACON             (TOOLTIPTEXT_BASE + 0x0307)
#define TOOLTIPTEXT_PROP_DEICE_SWITCH               (TOOLTIPTEXT_BASE + 0x0310)
#define TOOLTIPTEXT_AVIONICS_SWITCH                 (TOOLTIPTEXT_BASE + 0x0311)
#define TOOLTIPTEXT_SPIRIT_TOGGLE_ECU_WINDOW        (TOOLTIPTEXT_BASE + 0x0320)
#define TOOLTIPTEXT_SPIRIT_TOGGLE_EIC_WINDOW        (TOOLTIPTEXT_BASE + 0x0321)
#define TOOLTIPTEXT_SPIRIT_INCLINOMETER             (TOOLTIPTEXT_BASE + 0x0322)
#define TOOLTIPTEXT_SPIRIT_PITCH_INDICATOR          (TOOLTIPTEXT_BASE + 0x0323)
#define TOOLTIPTEXT_SPIRIT_PERISCOPE                (TOOLTIPTEXT_BASE + 0x0324)
#define TOOLTIPTEXT_SPIRIT_FUEL_PRESSURE            (TOOLTIPTEXT_BASE + 0x0325)
#define TOOLTIPTEXT_SPIRIT_PIONEER_COMPASS          (TOOLTIPTEXT_BASE + 0x0326)
#define TOOLTIPTEXT_VIMY_FUEL_PRESSURE              (TOOLTIPTEXT_BASE + 0x0328)
#define TOOLTIPTEXT_VIMY_FUEL_PUMP_PORT             (TOOLTIPTEXT_BASE + 0x0329)
#define TOOLTIPTEXT_VIMY_FUEL_PUMP_STARBOARD        (TOOLTIPTEXT_BASE + 0x032A)
#define TOOLTIPTEXT_VIMY_FUEL_VALVE_PORT            (TOOLTIPTEXT_BASE + 0x032B)
#define TOOLTIPTEXT_VIMY_FUEL_VALVE_STARBOARD       (TOOLTIPTEXT_BASE + 0x032C)
#define TOOLTIPTEXT_ALTERNATOR_SWITCH               (TOOLTIPTEXT_BASE + 0x0330)
#define TOOLTIPTEXT_VOLTMETER                       (TOOLTIPTEXT_BASE + 0x0331)
#define TOOLTIPTEXT_HEADING_INDICATOR_HEADING_2     (TOOLTIPTEXT_BASE + 0x0332)
#define TOOLTIPTEXT_ADF1_BEARING_TO_STATION         (TOOLTIPTEXT_BASE + 0x0333)
#define TOOLTIPTEXT_ADF_WINDOW                      (TOOLTIPTEXT_BASE + 0x0334)
#define TOOLTIPTEXT_FUEL_TRANSFER_PUMP              (TOOLTIPTEXT_BASE + 0x0335)
#define TOOLTIPTEXT_VEGA_FUEL_SELECTOR              (TOOLTIPTEXT_BASE + 0x0336)
#define TOOLTIPTEXT_AUTOPILOT_HEADING_REF           (TOOLTIPTEXT_BASE + 0x0337)
#define TOOLTIPTEXT_GENERATOR_SWITCH                (TOOLTIPTEXT_BASE + 0x0338)
#define TOOLTIPTEXT_FUEL_PRESSURE                   (TOOLTIPTEXT_BASE + 0x0339)
#define TOOLTIPTEXT_AIRSPEED_MPH                    (TOOLTIPTEXT_BASE + 0x033A)
#define TOOLTIPTEXT_WRIGHT_FLYER_DISTANCE           (TOOLTIPTEXT_BASE + 0x033B)
#define TOOLTIPTEXT_WRIGHT_FLYER_ELAPSED_TIME       (TOOLTIPTEXT_BASE + 0x033C)
#define TOOLTIPTEXT_ATTITUDE_GYRO_CAGE              (TOOLTIPTEXT_BASE + 0x033D)
#define TOOLTIPTEXT_ATTITUDE_GYRO_CAGE_2            (TOOLTIPTEXT_BASE + 0x033E)
#define TOOLTIPTEXT_ENGINE_PERCENT_RPM              (TOOLTIPTEXT_BASE + 0x033F)
#define TOOLTIPTEXT_ROTOR_PERCENT_RPM               (TOOLTIPTEXT_BASE + 0x0340)
#define TOOLTIPTEXT_TOGGLE_PEDESTAL_WINDOW          (TOOLTIPTEXT_BASE + 0x0341)
#define TOOLTIPTEXT_CARB_TEMPERATURE_CELSIUS        (TOOLTIPTEXT_BASE + 0x0342)
#define TOOLTIPTEXT_CARB_HEAT                       (TOOLTIPTEXT_BASE + 0x0343)
#define TOOLTIPTEXT_R22_MAIN_FUEL_PERCENT           (TOOLTIPTEXT_BASE + 0x0344)
#define TOOLTIPTEXT_R22_AUX_FUEL_PERCENT            (TOOLTIPTEXT_BASE + 0x0345)
#define TOOLTIPTEXT_R22_RT_TRIM                     (TOOLTIPTEXT_BASE + 0x0346)
#define TOOLTIPTEXT_R22_THROTTLE_PERCENT            (TOOLTIPTEXT_BASE + 0x0347)
#define TOOLTIPTEXT_ROTOR_GOVERNOR                  (TOOLTIPTEXT_BASE + 0x0348)
#define TOOLTIPTEXT_ROTOR_GOVERNOR_SWITCH           (TOOLTIPTEXT_BASE + 0x0349)
#define TOOLTIPTEXT_ROTOR_BRAKE                     (TOOLTIPTEXT_BASE + 0x034A)
#define TOOLTIPTEXT_ROTOR_BRAKE_HANDLE              (TOOLTIPTEXT_BASE + 0x034B)
#define TOOLTIPTEXT_ROTOR_CLUTCH                    (TOOLTIPTEXT_BASE + 0x034C)
#define TOOLTIPTEXT_ROTOR_CLUTCH_SWITCH             (TOOLTIPTEXT_BASE + 0x034D)
#define TOOLTIPTEXT_FUEL_VALVE                      (TOOLTIPTEXT_BASE + 0x034E)
#define TOOLTIPTEXT_TOGGLE_FLAPS_WINDOW             (TOOLTIPTEXT_BASE + 0x034F)
#define TOOLTIPTEXT_MANIFOLD_PRESSURE_PSI           (TOOLTIPTEXT_BASE + 0x0350)
#define TOOLTIPTEXT_ENG1_MANIFOLD_PRESSURE_PSI      (TOOLTIPTEXT_BASE + 0x0351)
#define TOOLTIPTEXT_ENG2_MANIFOLD_PRESSURE_PSI      (TOOLTIPTEXT_BASE + 0x0352)
#define TOOLTIPTEXT_ENG3_MANIFOLD_PRESSURE_PSI      (TOOLTIPTEXT_BASE + 0x0353)
#define TOOLTIPTEXT_ENG4_MANIFOLD_PRESSURE_PSI      (TOOLTIPTEXT_BASE + 0x0354)
#define TOOLTIPTEXT_BATTERY_LOAD                    (TOOLTIPTEXT_BASE + 0x0355)
#define TOOLTIPTEXT_SIDE_SLIP                       (TOOLTIPTEXT_BASE + 0x0356)
#define TOOLTIPTEXT_COMET_LEFT_FUEL_SELECTOR        (TOOLTIPTEXT_BASE + 0x0357)
#define TOOLTIPTEXT_COMET_RIGHT_FUEL_SELECTOR       (TOOLTIPTEXT_BASE + 0x0358)
#define TOOLTIPTEXT_COMET_CROSSFEED                 (TOOLTIPTEXT_BASE + 0x0359)
#define TOOLTIPTEXT_COMET_FRONT_TANK_FUEL_QTY       (TOOLTIPTEXT_BASE + 0x035A)
#define TOOLTIPTEXT_COMET_CENTER_TANK_FUEL_QTY      (TOOLTIPTEXT_BASE + 0x035B)
#define TOOLTIPTEXT_DC3_ENG1_FUEL_PRESSURE_LOW      (TOOLTIPTEXT_BASE + 0x035C)
#define TOOLTIPTEXT_DC3_ENG2_FUEL_PRESSURE_LOW      (TOOLTIPTEXT_BASE + 0x035D)
#define TOOLTIPTEXT_DC3_ENG1_OIL_PRESSURE_LOW       (TOOLTIPTEXT_BASE + 0x035E)
#define TOOLTIPTEXT_DC3_ENG2_OIL_PRESSURE_LOW       (TOOLTIPTEXT_BASE + 0x035F)
#define TOOLTIPTEXT_DC3_VOLTAGE_LOW                 (TOOLTIPTEXT_BASE + 0x0360)
#define TOOLTIPTEXT_DC3_VACUUM_LOW                  (TOOLTIPTEXT_BASE + 0x0361)
#define TOOLTIPTEXT_FORD_TRIMOTOR_FUEL_GAUGE_SELECTOR (TOOLTIPTEXT_BASE + 0x0362)
#define TOOLTIPTEXT_DC3_FUEL_GAUGE_SELECTOR         (TOOLTIPTEXT_BASE + 0x0363)
#define TOOLTIPTEXT_DC3_RMI1_SELECTOR               (TOOLTIPTEXT_BASE + 0x0364)
#define TOOLTIPTEXT_DC3_RMI2_SELECTOR               (TOOLTIPTEXT_BASE + 0x0365)
#define TOOLTIPTEXT_DC3_ADF_SIGNAL_STRENGTH         (TOOLTIPTEXT_BASE + 0x0366)
#define TOOLTIPTEXT_LANDING_GEAR_LATCH              (TOOLTIPTEXT_BASE + 0x0367)
#define TOOLTIPTEXT_WRIGHT_FLYER_ANEMOMETER         (TOOLTIPTEXT_BASE + 0x0368)
#define TOOLTIPTEXT_ENG1_RADIATOR_SHUTTER_PERCENT   (TOOLTIPTEXT_BASE + 0x0369)
#define TOOLTIPTEXT_ENG2_RADIATOR_SHUTTER_PERCENT   (TOOLTIPTEXT_BASE + 0x036A)
#define TOOLTIPTEXT_ENG1_GENERATOR_AMPS             (TOOLTIPTEXT_BASE + 0x036B)
#define TOOLTIPTEXT_ENG2_GENERATOR_AMPS             (TOOLTIPTEXT_BASE + 0x036C)
#define TOOLTIPTEXT_TOGGLE_OTHER_WINDOW             (TOOLTIPTEXT_BASE + 0x036D)
#define TOOLTIPTEXT_COLLECTIVE                      (TOOLTIPTEXT_BASE + 0x0370)
#define TOOLTIPTEXT_INERTIAL_SEPERATOR              (TOOLTIPTEXT_BASE + 0x0378)
#define TOOLTIPTEXT_NAV_GPS_SWITCH                  (TOOLTIPTEXT_BASE + 0x0380)
#define TOOLTIPTEXT_MASTER_SWITCH                   (TOOLTIPTEXT_BASE + 0x0384)
#define TOOLTIPTEXT_ENG1_FUEL_PRESSURE              (TOOLTIPTEXT_BASE + 0x0390)
#define TOOLTIPTEXT_ENG2_FUEL_PRESSURE              (TOOLTIPTEXT_BASE + 0x0391)
#define TOOLTIPTEXT_ENG3_FUEL_PRESSURE              (TOOLTIPTEXT_BASE + 0x0392)
#define TOOLTIPTEXT_ENG4_FUEL_PRESSURE              (TOOLTIPTEXT_BASE + 0x0393)
#define TOOLTIPTEXT_AUTOFEATHER_SWITCH              (TOOLTIPTEXT_BASE + 0x0398)
#define TOOLTIPTEXT_GPS_NRST_BUTTON                 (TOOLTIPTEXT_BASE + 0x03A0)
#define TOOLTIPTEXT_GPS_OBS_BUTTON                  (TOOLTIPTEXT_BASE + 0x03A1)
#define TOOLTIPTEXT_GPS_MSG_BUTTON                  (TOOLTIPTEXT_BASE + 0x03A2)
#define TOOLTIPTEXT_GPS_FPL_BUTTON                  (TOOLTIPTEXT_BASE + 0x03A3)
#define TOOLTIPTEXT_GPS_VNAV_BUTTON                 (TOOLTIPTEXT_BASE + 0x03A4)
#define TOOLTIPTEXT_GPS_PROC_BUTTON                 (TOOLTIPTEXT_BASE + 0x03A5)
#define TOOLTIPTEXT_GPS_CRSR_BUTTON                 (TOOLTIPTEXT_BASE + 0x03A6)
#define TOOLTIPTEXT_GPS_GROUP_KNOB                  (TOOLTIPTEXT_BASE + 0x03A7)
#define TOOLTIPTEXT_GPS_GROUP_KNOB_RIGHT            (TOOLTIPTEXT_BASE + 0x03A8)
#define TOOLTIPTEXT_GPS_GROUP_KNOB_LEFT             (TOOLTIPTEXT_BASE + 0x03A9)
#define TOOLTIPTEXT_GPS_PAGE_KNOB                   (TOOLTIPTEXT_BASE + 0x03AA)
#define TOOLTIPTEXT_GPS_PAGE_KNOB_RIGHT             (TOOLTIPTEXT_BASE + 0x03AB)
#define TOOLTIPTEXT_GPS_PAGE_KNOB_LEFT              (TOOLTIPTEXT_BASE + 0x03AC)
#define TOOLTIPTEXT_GPS_RANGE_UP                    (TOOLTIPTEXT_BASE + 0x03AE)
#define TOOLTIPTEXT_GPS_RANGE_DOWN                  (TOOLTIPTEXT_BASE + 0x03AF)
#define TOOLTIPTEXT_GPS_DIRECT_TO_BUTTON            (TOOLTIPTEXT_BASE + 0x03B0)
#define TOOLTIPTEXT_GPS_MENU_BUTTON                 (TOOLTIPTEXT_BASE + 0x03B1)
#define TOOLTIPTEXT_GPS_CLR_BUTTON                  (TOOLTIPTEXT_BASE + 0x03B2)
#define TOOLTIPTEXT_GPS_ENTER_BUTTON                (TOOLTIPTEXT_BASE + 0x03B3)
#define TOOLTIPTEXT_GPS_POWER_BUTTON                (TOOLTIPTEXT_BASE + 0x03B4)
#define TOOLTIPTEXT_GPS_TERR_BUTTON                 (TOOLTIPTEXT_BASE + 0x03B5)
#define TOOLTIPTEXT_GPS_QUIT_BUTTON                 (TOOLTIPTEXT_BASE + 0x03B6)
#define TOOLTIPTEXT_GPS_IN_BUTTON                   (TOOLTIPTEXT_BASE + 0x03B7)
#define TOOLTIPTEXT_GPS_OUT_BUTTON                  (TOOLTIPTEXT_BASE + 0x03B8)
#define TOOLTIPTEXT_GPS_PAD_UP                      (TOOLTIPTEXT_BASE + 0x03B9)
#define TOOLTIPTEXT_GPS_PAD_DOWN                    (TOOLTIPTEXT_BASE + 0x03BA)
#define TOOLTIPTEXT_GPS_PAD_LEFT                    (TOOLTIPTEXT_BASE + 0x03BB)
#define TOOLTIPTEXT_GPS_PAD_RIGHT                   (TOOLTIPTEXT_BASE + 0x03BC)
#define TOOLTIPTEXT_GPS_ROUTE_BUTTON                (TOOLTIPTEXT_BASE + 0x03BD)
#define TOOLTIPTEXT_GPS_DEFAULT_NAV_PAGE            (TOOLTIPTEXT_BASE + 0x03BE)
#define TOOLTIPTEXT_GPS_MAP_PAGE                    (TOOLTIPTEXT_BASE + 0x03BF)
#define TOOLTIPTEXT_GPS_AIRPORT_LOCATION_PAGE       (TOOLTIPTEXT_BASE + 0x03C0)
#define TOOLTIPTEXT_GPS_AIRPORT_RUNWAY_PAGE         (TOOLTIPTEXT_BASE + 0x03C1)
#define TOOLTIPTEXT_GPS_AIRPORT_FREQUENCY_PAGE      (TOOLTIPTEXT_BASE + 0x03C2)
#define TOOLTIPTEXT_GPS_AIRPORT_APPROACH_PAGE       (TOOLTIPTEXT_BASE + 0x03C3)
#define TOOLTIPTEXT_GPS_INTERSECTION_PAGE           (TOOLTIPTEXT_BASE + 0x03C4)
#define TOOLTIPTEXT_GPS_NDB_PAGE                    (TOOLTIPTEXT_BASE + 0x03C5)
#define TOOLTIPTEXT_GPS_VOR_PAGE                    (TOOLTIPTEXT_BASE + 0x03C6)
#define TOOLTIPTEXT_GPS_NEAREST_AIRPORT_PAGE        (TOOLTIPTEXT_BASE + 0x03C7)
#define TOOLTIPTEXT_GPS_NEAREST_INTERSECTION_PAGE   (TOOLTIPTEXT_BASE + 0x03C8)
#define TOOLTIPTEXT_GPS_NEAREST_NDB_PAGE            (TOOLTIPTEXT_BASE + 0x03C9)
#define TOOLTIPTEXT_GPS_NEAREST_VOR_PAGE            (TOOLTIPTEXT_BASE + 0x03CA)
#define TOOLTIPTEXT_GPS_NEAREST_AIRSPACE_PAGE       (TOOLTIPTEXT_BASE + 0x03CB)
#define TOOLTIPTEXT_GPS_FLIGHT_PLAN_PAGE            (TOOLTIPTEXT_BASE + 0x03CC)
#define TOOLTIPTEXT_GPS_PROCEDURES_PAGE             (TOOLTIPTEXT_BASE + 0x03CD)
#define TOOLTIPTEXT_LOW_ROTOR_RPM_ANNUNCIATOR       (TOOLTIPTEXT_BASE + 0x03CE)
#define TOOLTIPTEXT_ALTERNATOR_ANNUNCIATOR          (TOOLTIPTEXT_BASE + 0x03CF)
#define TOOLTIPTEXT_LOW_OIL_PRESSURE_ANNUNCIATOR    (TOOLTIPTEXT_BASE + 0x03D0)
#define TOOLTIPTEXT_LOW_FUEL_LEVEL_ANNUNCIATOR      (TOOLTIPTEXT_BASE + 0x03D1)
#define TOOLTIPTEXT_STARTER_ANNUNCIATOR             (TOOLTIPTEXT_BASE + 0x03D2)
#define TOOLTIPTEXT_MIXTURE_CUTOFF                  (TOOLTIPTEXT_BASE + 0x03D3)
#define TOOLTIPTEXT_ENG1_MIXTURE_CUTOFF             (TOOLTIPTEXT_BASE + 0x03D4)
#define TOOLTIPTEXT_ENG2_MIXTURE_CUTOFF             (TOOLTIPTEXT_BASE + 0x03D5)
#define TOOLTIPTEXT_ENG3_MIXTURE_CUTOFF             (TOOLTIPTEXT_BASE + 0x03D6)
#define TOOLTIPTEXT_ENG4_MIXTURE_CUTOFF             (TOOLTIPTEXT_BASE + 0x03D7)
#define TOOLTIPTEXT_THROTTLE_WRIGHT                 (TOOLTIPTEXT_BASE + 0x03D8)
#define TOOLTIPTEXT_FLAPS_ROCKER                    (TOOLTIPTEXT_BASE + 0x03D9)
#define TOOLTIPTEXT_DC3_LEFT_FUEL_SELECTOR          (TOOLTIPTEXT_BASE + 0x03E0)
#define TOOLTIPTEXT_DC3_RIGHT_FUEL_SELECTOR         (TOOLTIPTEXT_BASE + 0x03E1)
#define TOOLTIPTEXT_LIGHT_SWITCH_RECOGNITION        (TOOLTIPTEXT_BASE + 0x03E2)
#define TOOLTIPTEXT_AUTOBRAKE_SWITCH                (TOOLTIPTEXT_BASE + 0x03E3)
#define TOOLTIPTEXT_EMERGENCY_THROTTLE_PERCENT      (TOOLTIPTEXT_BASE + 0x03E4)
#define TOOLTIPTEXT_BACKUP_AIRSPEED_KNOTS           (TOOLTIPTEXT_BASE + 0x03E5)
#define TOOLTIPTEXT_BACKUP_AIRSPEED_KILOS           (TOOLTIPTEXT_BASE + 0x03E6)
#define TOOLTIPTEXT_BACKUP_ALTIMETER_METERS         (TOOLTIPTEXT_BASE + 0x03E7)
#define TOOLTIPTEXT_BACKUP_ALTIMETER_FEET           (TOOLTIPTEXT_BASE + 0x03E8)
#define TOOLTIPTEXT_BACKUP_ATTITUDE_INDICATOR_BANK_PITCH    (TOOLTIPTEXT_BASE + 0x03E9)
#define TOOLTIPTEXT_ALTERNATE_STATIC                (TOOLTIPTEXT_BASE + 0x03EA)
#define TOOLTIPTEXT_XPNDR_SET_CODE_1200             (TOOLTIPTEXT_BASE + 0x03EB)
#define TOOLTIPTEXT_XPNDR_STORE_STBY_CODE           (TOOLTIPTEXT_BASE + 0x03EC)
#define TOOLTIPTEXT_XPNDR_RECALL_STBY_CODE          (TOOLTIPTEXT_BASE + 0x03ED)
#define TOOLTIPTEXT_XPNDR_IDENT                     (TOOLTIPTEXT_BASE + 0x03EE)
#define TOOLTIPTEXT_XPNDR_REPLY                     (TOOLTIPTEXT_BASE + 0x03EF)
#define TOOLTIPTEXT_XPNDR_REPORTED_FLT_LVL          (TOOLTIPTEXT_BASE + 0x03F0)
#define TOOLTIPTEXT_XPNDR_REPORTED_METERS           (TOOLTIPTEXT_BASE + 0x03F1)
#define TOOLTIPTEXT_NAV_DISPLAY                     (TOOLTIPTEXT_BASE + 0x03F2)
#define TOOLTIPTEXT_NAV_DISPLAY_POWER               (TOOLTIPTEXT_BASE + 0x03F3)
#define TOOLTIPTEXT_NAV_DISPLAY_GO                  (TOOLTIPTEXT_BASE + 0x03F4)
#define TOOLTIPTEXT_NAV_DISPLAY_LEFT                (TOOLTIPTEXT_BASE + 0x03F5)
#define TOOLTIPTEXT_NAV_DISPLAY_UP                  (TOOLTIPTEXT_BASE + 0x03F6)
#define TOOLTIPTEXT_NAV_DISPLAY_DOWN                (TOOLTIPTEXT_BASE + 0x03F7)
#define TOOLTIPTEXT_NAV_DISPLAY_RIGHT               (TOOLTIPTEXT_BASE + 0x03F8)
#define TOOLTIPTEXT_CANOPY                          (TOOLTIPTEXT_BASE + 0x03F9)
#define TOOLTIPTEXT_MULTI_ENG_RPM                   (TOOLTIPTEXT_BASE + 0x03FA)
#define TOOLTIPTEXT_MULTI_ENG_MANIFOLD_PRESSURE_INHG        (TOOLTIPTEXT_BASE + 0x03FB)
#define TOOLTIPTEXT_MULTI_ENG_MANIFOLD_PRESSURE_PASCAL      (TOOLTIPTEXT_BASE + 0x03FC)
#define TOOLTIPTEXT_VHF_NAV1_COURSE                (TOOLTIPTEXT_BASE + 0x03FD)
#define TOOLTIPTEXT_BEAVER_RMI                     (TOOLTIPTEXT_BASE + 0x03FE)
#define TOOLTIPTEXT_BEAVER_FUEL_SELECTOR           (TOOLTIPTEXT_BASE + 0x03FF)
#define TOOLTIPTEXT_LOW_FUEL_PRESSURE_ANNUNCIATOR  (TOOLTIPTEXT_BASE + 0x0400)
#define TOOLTIPTEXT_BEAVER_FRONT_FUEL_GALLONS      (TOOLTIPTEXT_BASE + 0x0401)
#define TOOLTIPTEXT_BEAVER_CENTER_FUEL_GALLONS     (TOOLTIPTEXT_BASE + 0x0402)
#define TOOLTIPTEXT_BEAVER_REAR_FUEL_GALLONS       (TOOLTIPTEXT_BASE + 0x0403)
#define TOOLTIPTEXT_BEAVER_FRONT_FUEL_LITERS       (TOOLTIPTEXT_BASE + 0x0404)
#define TOOLTIPTEXT_BEAVER_CENTER_FUEL_LITERS      (TOOLTIPTEXT_BASE + 0x0405)
#define TOOLTIPTEXT_BEAVER_REAR_FUEL_LITERS        (TOOLTIPTEXT_BASE + 0x0406)
#define TOOLTIPTEXT_FUEL_PRESSURE_KGFSQCM          (TOOLTIPTEXT_BASE + 0x0407)
#define TOOLTIPTEXT_ENG_PRIMER                     (TOOLTIPTEXT_BASE + 0x0408)
#define TOOLTIPTEXT_BEAVER_FLAP_SELECTOR_EXTEND    (TOOLTIPTEXT_BASE + 0x0409)
#define TOOLTIPTEXT_BEAVER_FLAP_SELECTOR_RETRACT   (TOOLTIPTEXT_BASE + 0x040A)
#define TOOLTIPTEXT_BEAVER_FLAP_PUMP               (TOOLTIPTEXT_BASE + 0x040B)
#define TOOLTIPTEXT_ENGINE_ALTERNATE_AIR           (TOOLTIPTEXT_BASE + 0x040C)
#define TOOLTIPTEXT_MAGNETO_SWITCH_OFF             (TOOLTIPTEXT_BASE + 0x040D)
#define TOOLTIPTEXT_MAGNETO_SWITCH_LEFT            (TOOLTIPTEXT_BASE + 0x040E)
#define TOOLTIPTEXT_MAGNETO_SWITCH_RIGHT           (TOOLTIPTEXT_BASE + 0x040F)
#define TOOLTIPTEXT_MAGNETO_SWITCH_BOTH            (TOOLTIPTEXT_BASE + 0x0410)
#define TOOLTIPTEXT_STALL_WARNING                  (TOOLTIPTEXT_BASE + 0x0411)
#define TOOLTIPTEXT_VHF_NAV2_COURSE                (TOOLTIPTEXT_BASE + 0x0412)
#define TOOLTIPTEXT_LEFT_FUEL_WITH_AUX_TANK_GALLONS        (TOOLTIPTEXT_BASE + 0x0413)
#define TOOLTIPTEXT_RIGHT_FUEL_WITH_AUX_TANK_GALLONS       (TOOLTIPTEXT_BASE + 0x0414)
#define TOOLTIPTEXT_LEFT_FUEL_WITH_AUX_TANK_LITERS         (TOOLTIPTEXT_BASE + 0x0415)
#define TOOLTIPTEXT_RIGHT_FUEL_WITH_AUX_TANK_LITERS        (TOOLTIPTEXT_BASE + 0x0416)
#define TOOLTIPTEXT_WATER_BALLAST_DRAIN_VALVE      (TOOLTIPTEXT_BASE + 0x0417)
#define TOOLTIPTEXT_GENERATOR_WARNING_LIGHT        (TOOLTIPTEXT_BASE + 0x0418)
#define TOOLTIPTEXT_OIL_PRESSURE_WARNING_LIGHT     (TOOLTIPTEXT_BASE + 0x0419)
#define TOOLTIPTEXT_FUEL_PRESSURE_WARNING_LIGHT    (TOOLTIPTEXT_BASE + 0x041A)
#define TOOLTIPTEXT_ENGINE_FIRE_WARNING_LIGHT      (TOOLTIPTEXT_BASE + 0x041B)
#define TOOLTIPTEXT_ENG1_FUEL_PRESSURE_KGFSQCM     (TOOLTIPTEXT_BASE + 0x041C)
#define TOOLTIPTEXT_ENG2_FUEL_PRESSURE_KGFSQCM     (TOOLTIPTEXT_BASE + 0x041D)
#define TOOLTIPTEXT_ENG3_FUEL_PRESSURE_KGFSQCM     (TOOLTIPTEXT_BASE + 0x041E)
#define TOOLTIPTEXT_ENG4_FUEL_PRESSURE_KGFSQCM     (TOOLTIPTEXT_BASE + 0x041F)
#define TOOLTIPTEXT_GEAR_SWITCH                    (TOOLTIPTEXT_BASE + 0x0420)
#define TOOLTIPTEXT_CYCLE_NEXT_TARGET              (TOOLTIPTEXT_BASE + 0x0421)
#define TOOLTIPTEXT_CYCLE_PREVIOUS_TARGET          (TOOLTIPTEXT_BASE + 0x0422)
#define TOOLTIPTEXT_DISTANCE_TO_TARGET             (TOOLTIPTEXT_BASE + 0x0423)
#define TOOLTIPTEXT_TARGET_ALTITUDE                (TOOLTIPTEXT_BASE + 0x0424)
#define TOOLTIPTEXT_GAUGE_FIRE_PROTECTION           (TOOLTIPTEXT_BASE + 0x0425)
#define TOOLTIPTEXT_ENG1_FIRE_SWITCH                (TOOLTIPTEXT_BASE + 0x0426)
#define TOOLTIPTEXT_ENG2_FIRE_SWITCH                (TOOLTIPTEXT_BASE + 0x0427)
#define TOOLTIPTEXT_ENG3_FIRE_SWITCH                (TOOLTIPTEXT_BASE + 0x0428)
#define TOOLTIPTEXT_ENG4_FIRE_SWITCH                (TOOLTIPTEXT_BASE + 0x0429)
#define TOOLTIPTEXT_APU_FIRE_SWITCH                 (TOOLTIPTEXT_BASE + 0x042A)
#define TOOLTIPTEXT_APU_SWITCH                      (TOOLTIPTEXT_BASE + 0x042B)
#define TOOLTIPTEXT_APU_FAULT_LIGHT                 (TOOLTIPTEXT_BASE + 0x042C)
#define TOOLTIPTEXT_APU_ONOFF_LIGHT                 (TOOLTIPTEXT_BASE + 0x042D)
#define TOOLTIPTEXT_GAUGE_RADIO_CONTROL_HEAD        (TOOLTIPTEXT_BASE + 0x042E)
#define TOOLTIPTEXT_RADIO_COM1_SELECT_SWITCH        (TOOLTIPTEXT_BASE + 0x042F)
#define TOOLTIPTEXT_RADIO_COM1_SWAP_SWITCH          (TOOLTIPTEXT_BASE + 0x0430)
#define TOOLTIPTEXT_RADIO_COM2_SELECT_SWITCH        (TOOLTIPTEXT_BASE + 0x0431)
#define TOOLTIPTEXT_RADIO_COM2_SWAP_SWITCH          (TOOLTIPTEXT_BASE + 0x0432)
#define TOOLTIPTEXT_RADIO_NAV1_SELECT_SWITCH        (TOOLTIPTEXT_BASE + 0x0433)
#define TOOLTIPTEXT_RADIO_NAV1_SWAP_SWITCH          (TOOLTIPTEXT_BASE + 0x0434)
#define TOOLTIPTEXT_RADIO_NAV2_SELECT_SWITCH        (TOOLTIPTEXT_BASE + 0x0435)
#define TOOLTIPTEXT_RADIO_NAV2_SWAP_SWITCH          (TOOLTIPTEXT_BASE + 0x0436)
#define TOOLTIPTEXT_RADIO_ADF1_SELECT_SWITCH        (TOOLTIPTEXT_BASE + 0x0437)
#define TOOLTIPTEXT_RADIO_ADF2_SELECT_SWITCH        (TOOLTIPTEXT_BASE + 0x0438)
#define TOOLTIPTEXT_RADIO_XPDR_SELECT_SWITCH        (TOOLTIPTEXT_BASE + 0x0439)
#define TOOLTIPTEXT_RADIO_TUNER_KNOB                (TOOLTIPTEXT_BASE + 0x043A)
#define TOOLTIPTEXT_GAUGE_MFD                       (TOOLTIPTEXT_BASE + 0x043B)
#define TOOLTIPTEXT_SEATBELTS_SWITCH                (TOOLTIPTEXT_BASE + 0x043C)
#define TOOLTIPTEXT_NOSMOKING_SWITCH                (TOOLTIPTEXT_BASE + 0x043D)
#define TOOLTIPTEXT_ENG1_IGNITION_SWITCH            (TOOLTIPTEXT_BASE + 0x043E)
#define TOOLTIPTEXT_ENG2_IGNITION_SWITCH            (TOOLTIPTEXT_BASE + 0x043F)
#define TOOLTIPTEXT_GAUGE_TRIM                      (TOOLTIPTEXT_BASE + 0x0440)
#define TOOLTIPTEXT_GAUGE_BACKUP_VORADF             (TOOLTIPTEXT_BASE + 0x0441)
#define TOOLTIPTEXT_GAUGE_BACKUP_ALTASI             (TOOLTIPTEXT_BASE + 0x0442)
#define TOOLTIPTEXT_GAUGE_ECU                       (TOOLTIPTEXT_BASE + 0x0443)
#define TOOLTIPTEXT_GAUGE_EFIS_CONTROL_HEAD         (TOOLTIPTEXT_BASE + 0x0444)
#define TOOLTIPTEXT_GAUGE_OVERHEAD_PANEL            (TOOLTIPTEXT_BASE + 0x0445)
#define TOOLTIPTEXT_LEFT_LDG_LIGHT_RETRACT_SWITCH   (TOOLTIPTEXT_BASE + 0x0446)
#define TOOLTIPTEXT_RIGHT_LDG_LIGHT_RETRACT_SWITCH  (TOOLTIPTEXT_BASE + 0x0447)
#define TOOLTIPTEXT_ENG_ANTI_ICE_SWITCH             (TOOLTIPTEXT_BASE + 0x0448)
#define TOOLTIPTEXT_B737_LE_DEVICES_TEST_SWITCH     (TOOLTIPTEXT_BASE + 0x0449)
#define TOOLTIPTEXT_B737_MASTER_STARTER_ENG1        (TOOLTIPTEXT_BASE + 0x044A)
#define TOOLTIPTEXT_B737_MASTER_STARTER_ENG2        (TOOLTIPTEXT_BASE + 0x044B)
#define TOOLTIPTEXT_B737_DC_VOLTS_SWITCH            (TOOLTIPTEXT_BASE + 0x0450)
#define TOOLTIPTEXT_B737_GEN1_SWITCH                (TOOLTIPTEXT_BASE + 0x0451)
#define TOOLTIPTEXT_B737_GEN2_SWITCH                (TOOLTIPTEXT_BASE + 0x0452)
#define TOOLTIPTEXT_B737_APU1_SWITCH                (TOOLTIPTEXT_BASE + 0x0453)
#define TOOLTIPTEXT_B737_APU2_SWITCH                (TOOLTIPTEXT_BASE + 0x0454)
#define TOOLTIPTEXT_B737_EEC1_SWITCH                (TOOLTIPTEXT_BASE + 0x0455)
#define TOOLTIPTEXT_B737_EEC2_SWITCH                (TOOLTIPTEXT_BASE + 0x0456)
#define TOOLTIPTEXT_B737_HYD_ENG1_SWITCH            (TOOLTIPTEXT_BASE + 0x0457)
#define TOOLTIPTEXT_B737_HYD_ENG2_SWITCH            (TOOLTIPTEXT_BASE + 0x0458)
#define TOOLTIPTEXT_B737_HYD_ELEC_SWITCH            (TOOLTIPTEXT_BASE + 0x0459)
#define TOOLTIPTEXT_B737_ENG1_OFF_FLT_SWITCH        (TOOLTIPTEXT_BASE + 0x045A)
#define TOOLTIPTEXT_B737_ENG2_OFF_FLT_SWITCH        (TOOLTIPTEXT_BASE + 0x045B)
#define TOOLTIPTEXT_B737_ENG_START_SWITCH           (TOOLTIPTEXT_BASE + 0x045C)
#define TOOLTIPTEXT_TEMP_PLACEHOLDER_SYSTEM         (TOOLTIPTEXT_BASE + 0x045D)
#define TOOLTIPTEXT_B737_N1_KNOB                    (TOOLTIPTEXT_BASE + 0x045E)
#define TOOLTIPTEXT_C208_INERTIAL_SEPARATOR         (TOOLTIPTEXT_BASE + 0x045F)
#define TOOLTIPTEXT_C208_VOLT_AMMETER               (TOOLTIPTEXT_BASE + 0x0460)
#define TOOLTIPTEXT_XMISSION_PRESSURE_PSI           (TOOLTIPTEXT_BASE + 0x0461)
#define TOOLTIPTEXT_XMISSION_PRESSURE_KGFSQCM       (TOOLTIPTEXT_BASE + 0x0462)
#define TOOLTIPTEXT_XMISSION_TEMP_F                 (TOOLTIPTEXT_BASE + 0x0463)
#define TOOLTIPTEXT_XMISSION_TEMP_C                 (TOOLTIPTEXT_BASE + 0x0464)
#define TOOLTIPTEXT_FUEL_QUANTITY_GAL               (TOOLTIPTEXT_BASE + 0x0465)
#define TOOLTIPTEXT_FUEL_QUANTITY_L                 (TOOLTIPTEXT_BASE + 0x0466)
#define TOOLTIPTEXT_ENG_ELECTRICAL_LOAD             (TOOLTIPTEXT_BASE + 0x0467)
#define TOOLTIPTEXT_TURBINE_TEMPERATURE_F           (TOOLTIPTEXT_BASE + 0x0468)
#define TOOLTIPTEXT_TURBINE_TEMPERATURE_C           (TOOLTIPTEXT_BASE + 0x0469)
#define TOOLTIPTEXT_TOTAL_AIR_TEMPERATURE_CELSIUS   (TOOLTIPTEXT_BASE + 0x046A)
#define TOOLTIPTEXT_G_FORCE                         (TOOLTIPTEXT_BASE + 0x046B)
#define TOOLTIPTEXT_AUTOPILOT_ENGAGE                (TOOLTIPTEXT_BASE + 0x046C)
#define TOOLTIPTEXT_AUTOPILOT_DISCONNECT            (TOOLTIPTEXT_BASE + 0x046D)
#define TOOLTIPTEXT_TURB_ENG_IGNITION_SWITCH        (TOOLTIPTEXT_BASE + 0x046E)
#define TOOLTIPTEXT_EXTRA_FUEL_GAUGE_QUANTITY      (TOOLTIPTEXT_BASE + 0x046F)
#define TOOLTIPTEXT_DME_AUDIO_SELECTOR             (TOOLTIPTEXT_BASE + 0x0470)
#define TOOLTIPTEXT_A321_ACTIVE_FREQUENCY          (TOOLTIPTEXT_BASE + 0x0471)
#define TOOLTIPTEXT_A321_STBY_FREQUENCY            (TOOLTIPTEXT_BASE + 0x0472)
#define TOOLTIPTEXT_AUTO_LAND                      (TOOLTIPTEXT_BASE + 0x0473)
#define TOOLTIPTEXT_MASTER_WARN                    (TOOLTIPTEXT_BASE + 0x0474)
#define TOOLTIPTEXT_A321_PFD_CHRONO                (TOOLTIPTEXT_BASE + 0x0475)
#define TOOLTIPTEXT_TOGA                           (TOOLTIPTEXT_BASE + 0x0476)
#define TOOLTIPTEXT_A321_MASTER_STARTER_ENG1       (TOOLTIPTEXT_BASE + 0x0477)
#define TOOLTIPTEXT_A321_MASTER_STARTER_ENG2       (TOOLTIPTEXT_BASE + 0x0478)
#define TOOLTIPTEXT_A321_IGNITION_MODE             (TOOLTIPTEXT_BASE + 0x0479)
#define TOOLTIPTEXT_A321_INERTIAL_DATA_KNOB        (TOOLTIPTEXT_BASE + 0x047A)
#define TOOLTIPTEXT_A321_INERTIAL_DATA_SWITCH      (TOOLTIPTEXT_BASE + 0x047B)
#define TOOLTIPTEXT_A321_STD_BARO_PULL             (TOOLTIPTEXT_BASE + 0x047C)
#define TOOLTIPTEXT_SET_STD_BARO                   (TOOLTIPTEXT_BASE + 0x047D)
#define TOOLTIPTEXT_EMERGENCY_GEAR_HANDLE          (TOOLTIPTEXT_BASE + 0x047E)
#define TOOLTIPTEXT_GAUGE_EFIS                     (TOOLTIPTEXT_BASE + 0x047F)
#define TOOLTIPTEXT_GAUGE_ECAMS                    (TOOLTIPTEXT_BASE + 0x0480)
#define TOOLTIPTEXT_BACKUP_PFD                     (TOOLTIPTEXT_BASE + 0x0481)
#define TOOLTIPTEXT_CRJ_FLAP_LEVER                 (TOOLTIPTEXT_BASE + 0x0482)
#define TOOLTIPTEXT_A321_ELAC1                     (TOOLTIPTEXT_BASE + 0x0483)
#define TOOLTIPTEXT_A321_ELAC2                     (TOOLTIPTEXT_BASE + 0x0484)
#define TOOLTIPTEXT_A321_SEC1                      (TOOLTIPTEXT_BASE + 0x0485)
#define TOOLTIPTEXT_A321_SEC2                      (TOOLTIPTEXT_BASE + 0x0486)
#define TOOLTIPTEXT_A321_SEC3                      (TOOLTIPTEXT_BASE + 0x0487)
#define TOOLTIPTEXT_A321_FAC1                      (TOOLTIPTEXT_BASE + 0x0488)
#define TOOLTIPTEXT_A321_FAC2                      (TOOLTIPTEXT_BASE + 0x0489)
#define TOOLTIPTEXT_APU_VOLTS                      (TOOLTIPTEXT_BASE + 0x048A)
#define TOOLTIPTEXT_TURB_ENG2_IGNITION_SWITCH      (TOOLTIPTEXT_BASE + 0x048B)
#define TOOLTIPTEXT_LEAR45_POINTER1_SOURCE_SELECT_SWITCH  (TOOLTIPTEXT_BASE + 0x048C)
#define TOOLTIPTEXT_LEAR45_POINTER2_SOURCE_SELECT_SWITCH  (TOOLTIPTEXT_BASE + 0x048D)
#define TOOLTIPTEXT_CRJ_HPA_IN_BUTTON               (TOOLTIPTEXT_BASE + 0x048E)
#define TOOLTIPTEXT_CRJ_BRG1_BUTTON                 (TOOLTIPTEXT_BASE + 0x048F)
#define TOOLTIPTEXT_CRJ_BRG2_BUTTON                 (TOOLTIPTEXT_BASE + 0x0490)
#define TOOLTIPTEXT_CRJ_MFD_FORMAT                  (TOOLTIPTEXT_BASE + 0x0491)
#define TOOLTIPTEXT_CRJ_MAP_RANGE                   (TOOLTIPTEXT_BASE + 0x0492)
#define TOOLTIPTEXT_CRJ_HALF_BANK                   (TOOLTIPTEXT_BASE + 0x0493)
#define TOOLTIPTEXT_DISPLAY_CONTROL_PANEL           (TOOLTIPTEXT_BASE + 0x0494)
#define TOOLTIPTEXT_CRJ_BLEED_AIR_SOURCE            (TOOLTIPTEXT_BASE + 0x0495)
#define TOOLTIPTEXT_COURSE_SELECT                   (TOOLTIPTEXT_BASE + 0x0496)
#define TOOLTIPTEXT_COM_RECEIVE_ALL                 (TOOLTIPTEXT_BASE + 0x0497)
#define TOOLTIPTEXT_AUTOPILOT_MODE_ANNUNCIATOR      (TOOLTIPTEXT_BASE + 0x0498)
#define TOOLTIPTEXT_RMI_POINTER1                    (TOOLTIPTEXT_BASE + 0x0499)
#define TOOLTIPTEXT_RMI_POINTER2                    (TOOLTIPTEXT_BASE + 0x049A)
#define TOOLTIPTEXT_FIRE_WARNING_LIGHT              (TOOLTIPTEXT_BASE + 0x049B)
#define TOOLTIPTEXT_B737_EFIS_DISPLAY_MODE          (TOOLTIPTEXT_BASE + 0x049C)
#define TOOLTIPTEXT_B737_EFIS_DISPLAY_SCALE         (TOOLTIPTEXT_BASE + 0x049D)
#define TOOLTIPTEXT_B737_MFD_CENTERED               (TOOLTIPTEXT_BASE + 0x049E)
#define TOOLTIPTEXT_B737_POINTER1                   (TOOLTIPTEXT_BASE + 0x049F)
#define TOOLTIPTEXT_B737_POINTER2                   (TOOLTIPTEXT_BASE + 0x04A0)
#define TOOLTIPTEXT_B737_EFIS_SHOW_WAYPOINTS        (TOOLTIPTEXT_BASE + 0x04A1)
#define TOOLTIPTEXT_B737_EFIS_SHOW_AIRPORTS         (TOOLTIPTEXT_BASE + 0x04A2)
#define TOOLTIPTEXT_B737_EFIS_SHOW_NDB              (TOOLTIPTEXT_BASE + 0x04A3)
#define TOOLTIPTEXT_B737_EFIS_SHOW_VOR              (TOOLTIPTEXT_BASE + 0x04A4)
#define TOOLTIPTEXT_B737_AUTOPILOT_BANK_LIMIT       (TOOLTIPTEXT_BASE + 0x04A5)
#define TOOLTIPTEXT_NAV_FREQUENCY_SWAP              (TOOLTIPTEXT_BASE + 0x04A6)
#define TOOLTIPTEXT_COM_FREQUENCY_SWAP              (TOOLTIPTEXT_BASE + 0x04A7)
#define TOOLTIPTEXT_ALTITUDE_BUG_ADJUST             (TOOLTIPTEXT_BASE + 0x04A8)
#define TOOLTIPTEXT_COURSE_ADJUST                   (TOOLTIPTEXT_BASE + 0x04A9)
#define TOOLTIPTEXT_G1000_SOFTKEYS                  (TOOLTIPTEXT_BASE + 0x04AA)
#define TOOLTIPTEXT_VERTICAL_SPEED_KTS              (TOOLTIPTEXT_BASE + 0x04AB)
#define TOOLTIPTEXT_TOW_RELEASE                     (TOOLTIPTEXT_BASE + 0x04AC)
#define TOOLTIPTEXT_DG808S_VARIO_SWITCH             (TOOLTIPTEXT_BASE + 0x04AD)
#define TOOLTIPTEXT_DG808S_GYRO_SWITCH              (TOOLTIPTEXT_BASE + 0x04AE)
#define TOOLTIPTEXT_DG808S_VARIOMETER               (TOOLTIPTEXT_BASE + 0x04AF)
#define TOOLTIPTEXT_ANTISKID                        (TOOLTIPTEXT_BASE + 0x04B0)
#define TOOLTIPTEXT_B737_BACKUP_VOR_POINTER1        (TOOLTIPTEXT_BASE + 0x04B1)
#define TOOLTIPTEXT_B737_BACKUP_VOR_POINTER2        (TOOLTIPTEXT_BASE + 0x04B2)
#define TOOLTIPTEXT_B737_CLOCK_CHR                  (TOOLTIPTEXT_BASE + 0x04B3)
#define TOOLTIPTEXT_B737_CLOCK_DAT                  (TOOLTIPTEXT_BASE + 0x04B4)
#define TOOLTIPTEXT_B737_CLOCK_RUN_HLD              (TOOLTIPTEXT_BASE + 0x04B5)
#define TOOLTIPTEXT_B737_CLOCK_RST                  (TOOLTIPTEXT_BASE + 0x04B6)
#define TOOLTIPTEXT_B737_BACKUP_ATTITUDE_ILS_KNOB   (TOOLTIPTEXT_BASE + 0x04B7)
#define TOOLTIPTEXT_B737_BACKUP_ATTITUDE_CAL_KNOB   (TOOLTIPTEXT_BASE + 0x04B8)

//
// Common Flight Simulator Data Types
//

typedef void            *PVOID;
typedef PVOID           *PPVOID;

typedef unsigned    char    UINT8, *PUINT8, **PPUINT8;
typedef signed      char    SINT8, *PSINT8, **PPSINT8;
typedef unsigned    short   UINT16, *PUINT16, **PPUINT16;
typedef signed      short   SINT16, *PSINT16, **PPSINT16;
typedef signed      int     SINT32, *PSINT32, **PPSINT32;
typedef unsigned    char    VAR8, *PVAR8, **PPVAR8;
typedef unsigned    short   VAR16, *PVAR16, **PPVAR16;
typedef unsigned    int     VAR32, *PVAR32, **PPVAR32;

#ifndef _BASETSD_H_
typedef unsigned    int     UINT32, *PUINT32;
#endif
typedef unsigned    int     **PPUINT32;


#ifndef _BASETSD_H_ 
typedef unsigned __int64 UINT64, *PUINT64, **PPUINT64;
#endif

typedef signed __int64 SINT64, *PSINT64, **PPSINT64;

// 64-bit generic structure
typedef struct  VAR64
{
	VAR32   lo;
	VAR32   hi;
} VAR64, *PVAR64, **PPVAR64;

typedef struct  UIF64
{
	UINT32  f;
	UINT32  i;
} UIF64, *PUIF4, **PPUIF64;

typedef struct  SVAR64
{
	UINT32  lo;
	SINT32  hi;
} SVAR64, *PSVAR64, **PPSVAR64;

// boolean variables - hold TRUE/FALSE
typedef int     BOOL, *PBOOL, **PPBOOL;
typedef VAR8    BOOL8, *PBOOL8, **PPBOOL8;
typedef VAR16   BOOL16, *PBOOL16, **PPBOOL16;
typedef VAR32   BOOL32, *PBOOL32, **PPBOOL32;

// array of 1-bit boolean variables
typedef int     FLAGS, *PFLAGS, **PPFLAGS;
typedef VAR8    FLAGS8, *PFLAGS8, **PPFLAGS8;
typedef VAR16   FLAGS16, *PFLAGS16, **PPFLAGS16;
typedef VAR32   FLAGS32, *PFLAGS32, **PPFLAGS32;

// Enumeration variables - domain is subset of all possible values
typedef int     ENUM, *PENUM, **PPENUM;
typedef VAR8    ENUM8, *PENUM8, **PPENUM8;
typedef VAR16   ENUM16, *PENUM16, **PPENUM16;
typedef VAR32   ENUM32, *PENUM32, **PPENUM32;

// array index variables
typedef int     INDX, *PINDX, **PPINDX;
typedef VAR8    INDX8, *PINDX8, **PPINDX8;
typedef VAR16   INDX16, *PINDX16, **PPINDX16;
typedef VAR32   INDX32, *PINDX32, **PPINDX32;

// Identifier variables
typedef int     ID, *PID, **PPID;
typedef VAR8    ID8, *PID8, **PPID8;
typedef VAR16   ID16, *PID16, **PPID16;
typedef VAR32   ID32, *PID32, **PPID32;

// Error return variables
typedef VAR32   ERR, *PERR, **PPERR;

// Fractional variables
typedef UINT8   UFRAC8, *PUFRAC8, **PPUFRAC8;    // { 0..+1}
typedef SINT8   SFRAC8, *PSFRAC8, **PPSFRAC8;    // {-1..+1}
typedef UINT16  UFRAC16, *PUFRAC16, **PPUFRAC16; // { 0..+1}
typedef SINT16  SFRAC16, *PSFRAC16, **PPSFRAC16; // {-1..+1}
typedef UINT32  UFRAC32, *PUFRAC32, **PPUFRAC32; // { 0..+1}
typedef SINT32  SFRAC32, *PSFRAC32, **PPSFRAC32; // {-1..+1}

// Floating-point types
typedef double  FLOAT64, *PFLOAT64, **PPFLOAT64;
typedef float   FLOAT32, *PFLOAT32, **PPFLOAT32;

// Integer/Fraction structures both signed & unsigned
typedef UINT16  UIF16, *PUIF16, **PPUIF16;

typedef SINT16  SIF16, *PSIF16, **PPSIF16;

typedef UINT32  UIF32, *PUIF32, **PPUIF32;

typedef SINT32  SIF32, *PSIF32, **PPSIF32;

#ifdef _H2INC
typedef struct  UIF48
{
	UINT16  pad;
	UINT16  f;
	UINT32  i;
} UIF48, *PUIF48, **PPUIF48;

typedef struct  SIF48
{
	UINT16  pad;
	UINT16  f;
	SINT32  i;
} SIF48, *PSIF48, **PPSIF48;
#else
typedef union   UIF48
{
	struct
	{
		UINT16  pad;
		UINT16  f;
		UINT32  i;
	};

	UINT64      i64;

} UIF48, *PUIF48, **PPUIF48;

typedef union   SIF48
{
	struct
	{
		UINT16  pad;
		UINT16  f;
		SINT32  i;
	};

	SINT64      i64;

} SIF48, *PSIF48, **PPSIF48;
#endif


typedef SVAR64  SIF64, *PSIF64, **PPSIF64;

#if !defined(_WINNT_)               // NOT PORTABLE!
typedef char    CHAR;
typedef char    *PCHAR;             // single character
#endif
typedef PCHAR   *PPCHAR;            // pointer to pointer to char
typedef CHAR    STRING, *PSTRING, **PPSTRING;       // generic string of unknown length
typedef CHAR    STRINGZ, *PSTRINGZ, **PPSTRINGZ;    // generic string of unknown length, zero term
typedef const CHAR  *PCSTRINGZ, **PPCSTRINGZ;   // generic string of unknown length, zero term

typedef struct SUBSTRING
{
	PSTRINGZ    string;
	UINT32      leng;
}   SUBSTRING, *PSUBSTRING, **PPSUBSTRING;

typedef VAR16   BCD16, *PBCD16, **PPBCD16;      // 4-digit BCD number (0000-9999)
typedef VAR16   BCO16, *PBCO16, **PPBCO16;      // 4-digit Octal number (0000-7777) (in BCD)
typedef VAR32   BCD32, *PBCD32, **PPBCD32;      // 8-digit BCD number (00000000-99999999)
typedef VAR32   BCO32, *PBCO32, **PPBCO32;      // 8-digit Octal number (00000000-77777777) (in BCD)

// pseudodegree variables
typedef VAR8    ANGL8, *PANGL8, **PPANGL8;
typedef VAR16   ANGL16, *PANGL16, **PPANGL16;
typedef VAR32   ANGL32, *PANGL32, **PPANGL32;
#ifdef _H2INC
typedef struct  ANGL48
{
	UINT16  pad;
	UINT16  lo;
	UINT32  hi;
} ANGL48, *PANGL48, **PPANGL48;
#else
typedef union   ANGL48
{
	struct
	{
		UINT16  pad;
		UINT16  lo;
		UINT32  hi;
	};

	UINT64      i64;

} ANGL48, *PANGL48, **PPANGL48;
#endif

typedef ANGL48  SINT48, *PSINT48, **PPSINT48;

// radians
typedef FLOAT64 RADIANS;

// counter variables
typedef VAR8    CNTR8, *PCNTR8, **PPCNTR8;
typedef VAR16   CNTR16, *PCNTR16, **PPCNTR16;
typedef VAR32   CNTR32, *PCNTR32, **PPCNTR32;

// ring counter variables - modified by shifting instead of add/sub
typedef VAR8    RCNTR8, *PRCNTR8, **PPRCNTR8;
typedef VAR16   RCNTR16, *PRCNTR16, **PPRCNTR16;
typedef VAR32   RCNTR32, *PRCNTR32, **PPRCNTR32;

// Spare/Unused variables
typedef VAR8    SPARE8;
typedef VAR16   SPARE16;
typedef VAR32   SPARE32;

// version number data type
typedef struct  APP_VERSION
{
	UINT32  major;
	UINT32  minor;
	UINT32  build;
} APP_VERSION, *PAPP_VERSION, **PPAPP_VERSION;

// LatLon - used to store a position in 2D space
typedef struct
{
	SIF48       lat;
	ANGL48      lon;
} LATLON, *PLATLON, **PPLATLON;

// LatLon32 - used to store a position in 2D space (less accuracy)
typedef struct
{
	SINT32      lat;
	ANGL32      lon;
} LATLON32, *PLATLON32, **PPLATLON32;

// LatLonAlt - used to store a position in 3D space
typedef struct  LATLONALT
{
	SIF48       lat;
	ANGL48      lon;
	SIF48       alt;
} LATLONALT, *PLATLONALT, **PPLATLONALT;

typedef struct
{
	SINT32      lat;                // 32 bits of LAT (Meters)
	ANGL32      lon;                // 32 bits of LON
	SINT32      alt;                // 32 bits of ALT (Meters)
} LATLONALT32, *PLATLONALT32, **PPLATLONALT32;

typedef struct  _latlonalt_float32
{
	FLOAT32     lat;
	FLOAT32     lon;
	FLOAT32     alt;
} LATLONALT_FLOAT32, *PLATLONALT_FLOAT32, **PPLATLONALT_FLOAT32;

typedef struct _llf64               // llf64
{
	FLOAT64     lat;                // degrees (+/-90)
	FLOAT64     lon;                // degrees (+/-180)
} LLF64, *PLLF64, **PPLLF64;

typedef struct _llaf64              // llaf64
{
	FLOAT64     lat;                // degrees (+/-90)
	FLOAT64     lon;                // degrees (+/-180)
	FLOAT64     alt;                // meters (+/- a lot)
} LLAF64, *PLLAF64, **PPLLAF64;

// ZXY48 - used to mirror LatLonAlt with Lon as meters IIII.FF
typedef struct  ZXY48
{
	SIF48       z;
	SIF48       x;
	SIF48       y;
} ZXY48, *PZXY48, **PPZXY48;




// FLOAT64_VECTOR3 - <i,j,k> vector, hopefully normalized
typedef struct  _FLOAT64_VECTOR3
{
	FLOAT64     i;
	FLOAT64     j;
	FLOAT64     k;
}   FLOAT64_VECTOR3, *PFLOAT64_VECTOR3;

// FLOAT64_VECTOR2 - <i,j> vector
typedef struct  _FLOAT64_VECTOR2
{
	FLOAT64     i;
	FLOAT64     j;
}   FLOAT64_VECTOR2, *PFLOAT64_VECTOR2;

// PBH32 & PBH16 - rotation parameters of an object

typedef struct  PBH32
{
	ANGL32  pitch;
	ANGL32  bank;
	ANGL32  heading;
} PBH32, *PPBH32, **PPPBH32;

typedef struct  PBH16
{
	ANGL16  pitch;
	ANGL16  bank;
	ANGL16  heading;
} PBH16, *PPBH16, **PPPBH16;


// LATLONBOX - a set of lat and lon values that defines a rectangular boundary
typedef struct LATLONBOX
{
	SIF48   top;
	ANGL48  left;
	SIF48   bottom;
	ANGL48  right;
} LATLONBOX, *PLATLONBOX, **PPLATLONBOX;

// LATLONRECT32 - a set of lat and lon values that defines a rectangular boundary
typedef struct LATLONRECT32
{
	SINT32  north;
	SINT32  south;
	ANGL32  east;
	ANGL32  west;
} LATLONRECT32, *PLATLONRECT32, **PPLATLONRECT32;

// LATLONRECT32 - a set of lat and lon values that defines a rectangular boundary
typedef struct LATLONRECTF64
{
	FLOAT64 north;
	FLOAT64 south;
	FLOAT64 east;
	FLOAT64 west;
} LATLONRECTF64, *PLATLONRECTF64, **PPLATLONRECTF64;

typedef struct LLAPBH
{
	LATLONALT   lla;
	PBH32       pbh;
}  LLAPBH, *PLLAPBH, **PPLLAPBH;

// LATLONALTPBH - Combination of LATLONALT & PBH32
typedef union LATLONALTPBH
{
	struct
	{
		SIF48       lat;
		ANGL48      lon;
		SIF48       alt;
		ANGL32      pitch;
		ANGL32      bank;
		ANGL32      heading;
	};

	struct
	{
		LATLONALT   lla;
		PBH32       pbh;
	};
} LATLONALTPBH, *PLATLONALTPBH, **PPLATLONALTPBH;

// LATLONALTTPO - Time, Position, Orientation using LatLonAlt for Position
typedef struct  LATLONALTTPO
{
	UINT32      time;

	union
	{
		struct
		{
			SIF48       lat;
			ANGL48      lon;
			SIF48       alt;
			ANGL32      pitch;
			ANGL32      bank;
			ANGL32      heading;
		};

		struct
		{
			LATLONALT   lla;
			PBH32       pbh;
		};

		LATLONALTPBH    llapbh;
	};
} LATLONALTTPO, *PLATLONALTTPO, **PPLATLONALTTPO;

// Data structures for BGL manipulation
typedef struct LLA2416
{
	char    stream[3 + 3 + 2];
} LLA2416, *PLLA2416, **PPLLA2416;

typedef struct LLA2424
{
	char    stream[3 + 3 + 3];
} LLA2424, *PLLA2424, **PPLLA2424;

// coordinates of a point in a cartesion system
typedef struct  XYZ16
{
	SINT16  x;
	SINT16  y;
	SINT16  z;
} XYZ16, *PXYZ16, **PPXYZ16;

typedef struct  XYZ32
{
	SINT32  x;
	SINT32  y;
	SINT32  z;
} XYZ32, *PXYZ32, **PPXYZ32;

typedef struct  _xyz_float32
{
	FLOAT32     x;
	FLOAT32     y;
	FLOAT32     z;
} XYZ_FLOAT32, *PXYZ_FLOAT32, **PPXYZ_FLOAT32;


// Same as above, but uses east, altit, north for field names
typedef struct  EAN32
{
	SINT32  east;
	SINT32  altit;
	SINT32  north;
} EAN32, *PEAN32, **PPEAN32;

// Coordinates of a point on the XZ plane

typedef struct  XZF32
{
	FLOAT32 x;
	FLOAT32 z;
} XZF32, *PXZF32, **PPXZF32;

typedef struct  XZ32
{
	SINT32  x;
	SINT32  z;
} XZ32, *PXZ32, **PPXZ32;

// same as above, but uses east, north for field names

typedef struct  EN32
{
	SINT32  east;
	SINT32  north;
} EN32, *PEN32, **PPEN32;

// ANGLSINCOS16 structure to hold data for an angle and it's trig functions
typedef struct  ANGLSINCOS16
{
	ANGL16  angle;
	SFRAC16 sine;
	SFRAC16 cosine;
} ANGLSINCOS16, *PANGLSINCOS16, **PPANGLSINCOS16;

// XYZPBH - defines the position of an object
typedef union   XYZPBH
{
	struct
	{
		SINT32  x;
		SINT32  y;
		SINT32  z;
		ANGL16  pitch;
		ANGL16  bank;
		ANGL16  heading;
	};

	struct
	{
		XYZ32   xyz;
		PBH16   pbh;
	};
} XYZPBH, *PXYZPBH, **PPXYZPBH;

// XYZTPO - same as above, but starts with a time-stamp field
// Notes: these structures will need to be reordered when
//  H2INc is fixed!
typedef struct  XYZTPO
{
	UINT32  time;

	union
	{
		struct
		{
			SINT32  x;
			SINT32  y;
			SINT32  z;
			ANGL16  pitch;
			ANGL16  bank;
			ANGL16  heading;
		};

		struct
		{
			XYZ32   xyz;
			PBH16   pbh;
		};

		XYZPBH  xyzpbh;
	};
} XYZTPO, *PXYZTPO, **PPXYZTPO;


// XYZF64_  - XYZ point(vector) in floating point
// XYZF64 - XYZ vector which includes XYZF64 as a part of union
// (needed to be changed together with XYZF64)

struct  XYZF64_
{
	FLOAT64 x;
	FLOAT64 y;
	FLOAT64 z;
};

//  XYZF64, *PXYZF64, **PPXYZF64;

typedef struct  XYZF64
{
	union
	{
		struct { FLOAT64   lon; FLOAT64 alt;      FLOAT64 lat; };
		struct { FLOAT64     x; FLOAT64  y;       FLOAT64   z; };
		struct { FLOAT64 pitch; FLOAT64 heading;  FLOAT64 bank; };
	};
}
XYZF64, *PXYZF64, **PPXYZF64,
POS3_FLOAT64,
VEL3_FLOAT64, *PVEL3_FLOAT64, **PPVEL3_FLOAT64,
ACC3_FLOAT64,
ROT3_FLOAT64,
ROV3_FLOAT64,
ROA3_FLOAT64;

//-----------------------------------------------


// XYF64 - XY point (vector in 2d space) in floating point
typedef struct XYF64                // xyf64
{
	FLOAT64 x;
	FLOAT64 y;
} XYF64, *PXYF64, **PPXYF64;


//----------------------------------------------------
// XYZF32 - XYZW point(vector) in floating point
typedef struct XYZF32 {
	FLOAT32 x;
	FLOAT32 y;
	FLOAT32 z;
	FLOAT32 w;
}   XYZF32, *PXYZF32;

#ifdef __cplusplus
struct CXYZF32 :public XYZF32
{
	CXYZF32() { }
	CXYZF32(FLOAT32 _x, FLOAT32 _y, FLOAT32 _z, FLOAT32 _w = 1.0f) { x = _x; y = _y; z = _z; w = _w; }
};
#endif
//---------------------------------------------------


typedef FLOAT64 ANGLF64;    // radians

// PBHF64 - Pitch/bank/heading in floating point radians
typedef struct PBHF64 {
	ANGLF64 pitch;
	ANGLF64 bank;
	ANGLF64 heading;
} PBHF64, *PPBHF64, **PPPBHF64;

// FLOAT32 matrix
typedef struct MATRIXF32
{
	union
	{
		struct { XYZF32 x, y, z, w; };
		struct
		{
			FLOAT32 m00, m01, m02, m03;
			FLOAT32 m10, m11, m12, m13;
			FLOAT32 m20, m21, m22, m23;
			FLOAT32 m30, m31, m32, m33;
		};
		struct
		{
			FLOAT32 _11, _12, _13, _14;
			FLOAT32 _21, _22, _23, _24;
			FLOAT32 _31, _32, _33, _34;
			FLOAT32 _41, _42, _43, _44;
		};
		FLOAT32 m[4][4];
		FLOAT32 mat[4][4];
	};
#ifdef __cplusplus
	FLOAT32& operator()(int iRow, int iColumn) { return m[iRow][iColumn]; }
	const FLOAT32& operator()(int iRow, int iColumn) const { return m[iRow][iColumn]; }
#endif
} MATRIXF32, *PMATRIXF32;

// map ROTMTRXF64 to a MATRIXF32
typedef MATRIXF32  ROTMTRXF64, *PROTMTRXF64;
// map QUATERN_ROTMAT to a MATRIXF32
typedef MATRIXF32  QUATERN_ROTMAT, *PQUATERN_ROTMAT;

//  2D data structures

typedef SINT32  UNIPIX, *PUNIPIX, **PPUNIPIX;   // universal pixel (X=0-8191    Y=0-6143}
typedef SINT32  PIXEL, *PPIXEL, **PPPIXEL;      // screen pixel
typedef VAR32  UNICOL, *PUNICOL, **PPUNICOL;    // universal color code

typedef struct  RGB15
{
	UINT16      blue : 5;
	UINT16      green : 5;
	UINT16      red : 5;
	UINT16      alpha : 1;
} RGB15, *PRGB15, **PPRGB15;

typedef struct  RGB16
{
	UINT16      blue : 5;
	UINT16      green : 6;
	UINT16      red : 5;
} RGB16, *PRGB16, **PPRGB16;


typedef struct  RGB24
{
	UINT8       blue;
	UINT8       green;
	UINT8       red;
} RGB24, *PRGB24, **PPRGB24;

typedef struct  RGB32
{
	UINT8       blue;
	UINT8       green;
	UINT8       red;
	UINT8       junk;
} RGB32, *PRGB32, **PPRGB32;

typedef struct  RGBA
{
	UINT8       blue;
	UINT8       green;
	UINT8       red;
	UINT8       alpha;
} RGBA, *PRGBA, **PPRGBA;

typedef struct  RGB         // this is used by the 2d system and stuff
{                           // note that red and blue are backwards from
	UINT8       red;        // what windows expects
	UINT8       green;
	UINT8       blue;
} RGB, *PRGB, **PPRGB;

typedef struct  RGBAF
{
	FLOAT32     r;
	FLOAT32     g;
	FLOAT32     b;
	FLOAT32     a;
} RGBAF, *PRGBAF;

typedef struct  XY
{
	SINT32  x;
	SINT32  y;
} XY, *PXY, **PPXY;

typedef struct  UNIPOINT
{
	UNIPIX  x;
	UNIPIX  y;
} UNIPOINT, *PUNIPOINT, **PPUNIPOINT;

typedef struct  UNIBOX
{
	UNIPIX  x;
	UNIPIX  y;
	UNIPIX  size_x;
	UNIPIX  size_y;
} UNIBOX, *PUNIBOX, **PPUNIBOX;

typedef struct  UNIRECT
{
	UNIPIX  left;
	UNIPIX  top;
	UNIPIX  right;
	UNIPIX  bottom;
} UNIRECT, *PUNIRECT, **PPUNIRECT;

typedef struct  PIXPOINT
{
	PIXEL x;
	PIXEL y;
} PIXPOINT, *PPIXPOINT, **PPPIXPOINT;

typedef const PIXPOINT *PCPIXPOINT, **PPCPIXPOINT;

typedef struct  PIXBOX
{
	PIXEL x;
	PIXEL y;
	PIXEL size_x;
	PIXEL size_y;
} PIXBOX, *PPIXBOX, **PPPIXBOX;

typedef struct  PIXRECT
{
	PIXEL left;
	PIXEL top;
	PIXEL right;
	PIXEL bottom;
} PIXRECT, *PPIXRECT, **PPPIXRECT;


typedef struct  BOUNDSRECT
{
	SIF48   N;                  //  north boundary
	SIF48   S;                  //  south boundary
	ANGL48  E;                  // east boundary
	ANGL48  W;                  // west boundary

} BOUNDSRECT, *PBOUNDSRECT, **PPBOUNDSRECT;


typedef struct
{
	SINT32  north;              // north boundary
	SINT32  south;              // south boundary
	SINT32  east;               // east boundary
	SINT32  west;               // west boundary

} BOUNDSRECT32, *PBOUNDSRECT32, **PPBOUNDSRECT32;


// LLA_INFO - this struct contains a LATLONALT param and a user definable PVOID ptr
//  useful for an array of LATLONALTs that you also want to associate some additional data with
typedef struct  LLA_INFO
{
	union
	{
		struct
		{
			SIF48       lat;
			ANGL48      lon;
			SIF48       alt;
		};
		LATLONALT   lla;
	};
	PVOID               data_ptr;
} LLA_INFO, *PLLA_INFO, **PPLLA_INFO;


// enum constants for IMAGE.format
typedef enum IMG_FORMAT
{
	IMG_8_BIT_MONOCHROME = 0,
	IMG_8_BIT_INDEXED,
	IMG_15_BIT,             // 1555
	IMG_16_BIT,             // 565
	IMG_16A_BIT,            // 4444
	IMG_24_BIT,             // 888
	IMG_32_BIT,             // 888
	IMG_32A_BIT,            // 8888
	IMG_DXT1,               // DirectX Texture Compression DXT1
	IMG_DXT3,               // DirectX Texture Compression DXT3
	IMG_DUDV,               // Pertubation data
	IMG_MAX                 // keep this last
} IMG_FORMAT, *PIMG_FORMAT, **PPIMG_FORMAT;

// IMAGE - structure containing info pertaining to an offscreen image buffer
//         NOTE this structure should not change size compared to the FS98
//         version because gauges use this structure
typedef struct  IMAGE
{
	IMG_FORMAT  format : 16;  // bit format for this image
	UINT32      flags : 16;   // flags for this image
	PIXPOINT    dim;        // offscreen buffer size (viewable area)
	UINT32      pitch;      // offset to add to advance one scan line in image buffer
	RGBA*       palette;    // palette for IMG_8_BIT_INDEXED images (was rowtbl)
	UINT32      len;        // xxx
	PCHAR       image;      // pointer to image buffer
	PVOID       pdx;        // pointer to directx information
} IMAGE, *PIMAGE, **PPIMAGE;

typedef const IMAGE *PCIMAGE, **PPCIMAGE;

#define IMAGE_XY(p,x,y) ((BYTE *)(p)->image + ((p)->pitch * (y)) + (x))
#define IMAGE_PTR(p,y)  IMAGE_XY(p,0,y)

// View Dir enum for select_view
typedef enum    VIEW_DIR {
	VIEW_DIR_FORWARD,
	VIEW_DIR_FORWARD_RIGHT,
	VIEW_DIR_RIGHT,
	VIEW_DIR_REAR_RIGHT,
	VIEW_DIR_REAR,
	VIEW_DIR_REAR_LEFT,
	VIEW_DIR_LEFT,
	VIEW_DIR_FORWARD_LEFT,
	VIEW_DIR_DOWN,
	VIEW_DIR_FORWARD_UP,
	VIEW_DIR_FORWARD_RIGHT_UP,
	VIEW_DIR_RIGHT_UP,
	VIEW_DIR_REAR_RIGHT_UP,
	VIEW_DIR_REAR_UP,
	VIEW_DIR_REAR_LEFT_UP,
	VIEW_DIR_LEFT_UP,
	VIEW_DIR_FORWARD_LEFT_UP,
	VIEW_DIR_UP,
	VIEW_DIR_AUX_00,
	VIEW_DIR_AUX_01,
	VIEW_DIR_AUX_02,
	VIEW_DIR_AUX_03,
	VIEW_DIR_AUX_04,
	VIEW_DIR_AUX_05,
	VIEW_DIR_MAX
} VIEW_DIR, *PVIEW_DIR, **PPVIEW_DIR;


// BOUNDBOX - this structure is used extensively by the FrontEnd system, and by the symbolic map
typedef struct BOUNDBOX
{
	SIF32   N;      // northern latitude boundary
	SIF32   S;      // southern latitude boundary
	ANGL32  E;      // eastern longitude boundary
	ANGL32  W;      // western longitude boundary
} BOUNDBOX, *PBOUNDBOX, **PPBOUNDBOX;

typedef struct _engine_fuel_vars
{
	ENUM        tank_selector;          //what tanks are selected to draw from
	FLAGS       tanks_used;             //what tanks are actually being drawn from
	UINT32      number_of_tanks_used;   //# of tanks being drawn from
	FLOAT64     fuelflow_pph;           //lbs/hr
	BOOL32      fuel_available;         //True or False in selected tanks

} ENG_FUEL_VARS, *PENG_FUEL_VARS, **PPENG_FUEL_VARS;

typedef struct  ENG
{
	SINT16      throttle_lvr_pos;       // throttle position (0-16384=>0-100%throttle position)
	UINT16      propeller_lvr_pos;      // propeller lever position (0-16384=>0-100%prop setting)
	UINT16      mixture_lvr_pos;        // mixture position (0-16384=>0-100%mixture position)
	ENUM16      starter;                // starter switch position

	BOOL16      combustion;             // combustion flag (combustion = TRUE)
	UINT16      n1_rpm;                 // gas producer RPM (0-16384=>0-100%RPM)
	UINT16      n2_rpm;                 // power RPM (0-16384=>0-100%RPM)
	UINT16      ff_pph;                 // fuel flow (0-16384=>0-100%)PPH (normalized to Lear 3500 lbs thrust)
	UINT32      ff_pph_actual;          // fuel flow (0-16384=>0-100%)PPH
	UINT16      ff_pph_ssl;             // fuel flow (0-16384=>0-100%)PPH, Standard sea level conditions
	UINT16      torque;                 // torque produced by engine (FT*LBS)
	UINT16      absorbed_torque;        // torque absorbed by propeller (FT*LBS)
	SINT32      thrust_lbs;             // thrust (LBS)
	ANGL16      blade_angle_pdeg;       // propeller blade angle (PDEG)

	BOOL16      lf_mag;                 // left magneto status (activated = 1)
	BOOL16      rt_mag;                 // right magneto status (activated = 1)
	BOOL16      anti_ice;               // anti ice switch
	UINT16      epr_bug_pos;            // epr governed engine setting (0-16384=>0-100%epr setting)
	UINT16      egt_bug_pos;            // egt governed engine setting (0-16384=>0-100%egt setting)

	UINT16      oil_tmp;                // oil temperature (degC) * 16384
	UINT16      oil_prs;                // oil pressure (PSI) * 16384
	UINT16      epr;                    // engine pressure ratio (0-65536=>0-6.4)
	UINT16      egt_degC;               // exhaust gas temperature (degC)
	UINT16      manifold_pressure;      // manifold pressure (0-65536=>0-64)

	SINT16      x_pos;                  // lateral engine position (IN)
	SINT16      y_pos;                  // vertical engine position (IN)
	SINT16      z_pos;                  // longitudinal engine position (IN)

	UINT16      rpm_scaler;             // *16384 gives actual prop rpm in dx (= 65535 for jets)
	PVOID       ext_ptr;                // engine structure extension pointer

	UINT32      oil_quantity;           //oil quantity (0-16384 => 0-100%)
	UINT32      engine_vibration;       //oil quantity (0-16384 => 0-5)
	UINT32      hydraulic_pressure;     //hydraulic pressure (0-16384 => 0-4 psi)
	UINT32      hydraulic_quantity;     //hydraulic quantity (0-16384 => 0-100%)
	FLOAT64     induced_velocity;       //induced velocity from propwash or jet blast

	FLOAT64     cyl_head_temp_degF;     //cylinder head temperature (deg F)

	UINT32      tot_ind;                // TOT * 16384, deg C
	UINT32      torque_ind;             // TQ * 16384, %
	UINT32      Wfpress_ind;            // Fuel Pressure * 16384, psi
	UINT32      electric_load;          // electrical load * 16384, % max load
	UINT32      xmsn_pressure;          // xmsn pressure * 16384, psi
	UINT32      xmsn_temperature;       // transmission temp * 16384, deg C
	UINT32      rotor_rpm;              // main rotor speed * 16384, % max

	ENG_FUEL_VARS   fuel_vars;

} ENG, *PENG, **PPENG;


// mouse sensitivity structure
typedef struct  SENSEBLOCK
{
	PIXEL       left;
	PIXEL       top;
	PIXEL       right;
	PIXEL       bottom;
	PVOID       routine;
} SENSEBLOCK, *PSENSEBLOCK, **PPSENSEBLOCK;

typedef enum    XVOR_TYPE
{
	VOR_UNKNOWN = 0,
	VOR_TERMINAL = 1,
	VOR_LOW_ALTITUDE = 2,
	VOR_LOW_ALT = 2,
	VOR_HIGH_ALTITUDE = 3,
	VOR_HIGH_ALT = 3,
	VOR_ILS = 4,
	VOR_VOT = 5,
} XVOR_TYPE, *PXVOR_TYPE, **PPXVOR_TYPE;


typedef struct
{
	ENUM32      code;               // code value
	PSTRINGZ    text;               // text value
} LOOKUP_TABLE, *PLOOKUP_TABLE, **PPLOOKUP_TABLE;

typedef struct _xyz_minmax
{
	XYZF32 xyzMin;
	XYZF32 xyzMax;
} XYZ_MINMAX, *PXYZ_MINMAX, **PPXYZ_MINMAX;


typedef struct _min_max
{
	FLOAT32 fMin;
	FLOAT32 fMax;
} MIN_MAX, *PMIN_MAX, **PPMIN_MAX;


typedef struct _uv
{
	FLOAT32 u;
	FLOAT32 v;
} UV, *PUV, **PPUV;

typedef struct _uv_info
{
	UV uv[2];
} UV_INFO, *PUV_INFO, **PPUV_INFO;


// fuel_tank_selector enum values
typedef enum    FUEL_SELECT
{
	FUEL_TANK_SELECTOR_OFF = 0,
	FUEL_TANK_SELECTOR_ALL = 1,
	FUEL_TANK_SELECTOR_LEFT = 2,
	FUEL_TANK_SELECTOR_RIGHT = 3,
	FUEL_TANK_SELECTOR_LEFT_AUX = 4,
	FUEL_TANK_SELECTOR_RIGHT_AUX = 5,
	FUEL_TANK_SELECTOR_CENTER = 6,
	FUEL_TANK_SELECTOR_CENTER2 = 7,
	FUEL_TANK_SELECTOR_CENTER3 = 8,
	FUEL_TANK_SELECTOR_EXTERNAL1 = 9,
	FUEL_TANK_SELECTOR_EXTERNAL2 = 10,
	FUEL_TANK_SELECTOR_RIGHT_TIP = 11,
	FUEL_TANK_SELECTOR_LEFT_TIP = 12,
	FUEL_TANK_SELECTOR_CROSSFEED = 13,
	FUEL_TANK_SELECTOR_CROSSFEED_L2R = 14,
	FUEL_TANK_SELECTOR_CROSSFEED_R2L = 15,
	FUEL_TANK_SELECTOR_BOTH = 16,
	FUEL_TANK_SELECTOR_EXTERNAL_ALL = 17,
	FUEL_TANK_SELECTOR_ISOLATE = 18,

	FUEL_TANK_MAXIMUM
} FUEL_SELECT, *PFUEL_SELECT, **PPFUEL_SELECT;

// cross feed enum values
typedef enum    CROSS_FEED_SELECT
{
	CROSS_FEED_OFF = 0,
	CROSS_FEED_LEFT_TO_RIGHT,
	CROSS_FEED_RIGHT_TO_LEFT,

	CROSS_FEED_MAX
} CROSS_FEED, *PCROSS_FEED, **PPCROSS_FEED;

// prop type enum values
typedef enum PROP_TYPE
{
	PROP_TYPE_NONE,
	PROP_TYPE_FIXED,
	PROP_TYPE_MANUAL,
	PROP_TYPE_CHOICE
} PROP_TYPE;

// units enum values
typedef enum UNITS_OF_MEASURE
{
	ENGLISH_UNITS,
	METRIC_UNITS_ALT_FEET,
	METRIC_UNITS_ALT_METER
} UNITS_OF_MEASURE;

//
//  Panel System types and defines
//

//  Token list
//

    typedef enum    GAUGE_TOKEN
    {
		MODULE_VAR_NONE = 0,
		TICK18 = 1, 			// inc'd at 18hz. used for general purpose timing
		GROUND_ALTITUDE = 2, 			// ground level altitude ( 1/256 meter/unit)
		CLOCK_HOUR = 3, 			// local clock time, hours {0..23}
		CLOCK_MINUTE = 4, 			// local clock time, minutes {0..59}
		CLOCK_SECOND = 5, 			// local clock time, seconds {0..59}
		ZULU_HOUR = 6, 			// delta time from GMT
		ZULU_MINUTE = 7, 			// delta time from GMT
		ZULU_DAY = 8, 			// GMT day of year (1 - 365/366)
		ZULU_YEAR = 9, 			// GMT year
		PANEL_LIGHTS = 10, 			// panel_lights:  g_lightState & LIGHT_PANEL_MASK, old global panel_lights
		STROBE_LIGHTS = 11, 			// strobe_lights: g_lightState & LIGHT_STROBE_MASK, old global strobe_lights
		PITOT_HEAT = 13, 			// TRUE=pitot heat on
		LANDING_LIGHTS = 14, 			// landing_lights: g_lightState & LIGHT_LANDING_MASK, old global landing_lights
		SMOKE_ENABLE = 15, 			// TRUE=smoke system active
		ADF_FREQUENCY = 16, 			// |
		ADF_500_HZ_TUNABLE = 17, 			// ADF frequency
		ADF_EXTENDED_FREQUENCY = 18, 			// |
		ADF_IDENTITY = 19, 			// 
		ADF_SIGNAL_STRENGTH = 20, 			// 
		ADF_MORSE_IDENT = 21, 			// 
		ADF_CARD_RADIAL = 22, 			// integer degrees 0-360
		COM_FREQUENCY = 23, 			// BCD COM radio freq
		NAV1_FREQUENCY = 24, 			// BCD NAV1 radio freq
		NAV1_VOR_RADIAL = 25, 			// 
		NAV1_MORSE_IDENT = 26, 			// 
		NAV2_FREQUENCY = 27, 			// BCD NAV2 radio freq
		NAV2_VOR_RADIAL = 28, 			// 
		NAV2_MORSE_IDENT = 29, 			// 
		DME1_DISPLAY_TYPE = 30, 			// FALSE=distance readout, TRUE=speed in kts readout
		DME1_DISTANCE = 31, 			// DME1 distance readout
		DME1_SPEED = 32, 			// DME1 speed readout
		DME1_MORSE_IDENT = 33, 			// 
		DME2_DISPLAY_TYPE = 34, 			// FALSE=distance readout, TRUE=speed in kts readout
		DME2_DISTANCE = 35, 			// DME2 distance readout
		DME2_SPEED = 36, 			// DME2 speed readout
		DME2_MORSE_IDENT = 37, 			// 
		TRANSPONDER_CODE = 38, 			// Octal Transponder code {0000 - 7777}
		ADF_NEEDLE = 39, 			// 16 bit pseudodegrees
		VOR1_NEEDLE = 40, 			// +-127 correspond to +-10 if VOR or to +-2.5 if localiser.
		VOR1_GS_NEEDLE = 41, 			// +-120 correspond to +-0.7
		VOR1_BACK_COURSE_FLAGS = 42, 			// Status of VOR1 (see BC_FLAG_ in FS6DEF.H)
		VOR1_TF_FLAG = 43, 			// 
		VOR1_GS_FLAG = 44, 			// 
		VOR1_OBI = 45, 			// 
		VOR1_CODE = 46, 			// 
		VOR1_IDENTITY = 47, 			// 
		VOR1_MAG_VAR = 48, 			// 
		VOR1_SIGNAL_STRENGTH = 49, 			// 
		VOR2_NEEDLE = 50, 			// 
		VOR2_BACK_COURSE_FLAGS = 51, 			// Status of VOR2 (see BC_FLAG_ in FS6DEF.H)
		VOR2_TF_FLAG = 52, 			// 
		VOR2_GS_FLAG = 53, 			// 
		VOR2_OBI = 54, 			// 
		VOR2_CODE = 55, 			// 
		VOR2_IDENTITY = 56, 			// 
		VOR2_MAG_VAR = 57, 			// 
		VOR2_SIGNAL_STRENGTH = 58, 			// 
		VOR2_BEARING_DEGREES = 59, 			// 
		AIRCRAFT_ON_GROUND = 60, 			// TRUE=aircraft is on the ground
		AIRSPEED = 61, 			// Airspeed - Indicated or True based on GLOBALP->indicated_aspd
		AIRSPEED_TRUE_CALIBRATE = 62, 			// Degrees rotation of the tas calibration card
		BARBER_POLE_ASPD = 63, 			// Movable Red-line airspeed  {1/128th knot/unit}
		MACH = 64, 			// mach, 64k=3.2 mach (would be better as UIF32)
		VERTICAL_SPEED = 65, 			// Velocity Up, vertical speed {1/256th m/sec}
		MACH_MAX_OPERATE = 66, 			// 
		STALL_WARNING = 67, 			// TRUE=5 knots before stall
		OVERSPEED_WARNING = 68, 			// TRUE=overspeed warning
		TURN_COORDINATOR_BALL_POS = 69, 			// turn coordinator ball position {-127..0..127}
		DELTA_HEADING_RATE = 70, 			// 
		PLANE_LATITUDE = 71, 			// current position of our aircraft
		PLANE_LONGITUDE = 72, 			// current position of our aircraft
		PLANE_ALTITUDE = 73, 			// current position of our aircraft
		ALT_FROM_BAROMETRIC_PRESSURE = 74, 			// 
		KOHLSMAN_SETTING_MB = 75, 			// knob position millibars
		KOHLSMAN_SETTING_HG = 76, 			// knob position inches of hg
		PLANE_PITCH_DEGREES = 77, 			// 
		ATTITUDE_INDICATOR_PITCH_DEGREES = 78, 			// 
		PLANE_BANK_DEGREES = 79, 			// 
		ATTITUDE_INDICATOR_BANK_DEGREES = 80, 			// 
		PLANE_HEADING_DEGREES_MAGNETIC = 81, 			// 
		MAGNETIC_VAR = 82, 			// magnetic north vs. true north angular error
		WHISKEY_COMPASS_DEGREES = 83, 			// True degrees
		PLANE_HEADING_DEGREES_GYRO = 84, 			// 
		GYRO_DRIFT_ERROR = 85, 			// 
		PLANE_HEADING_DEGREES_TRUE = 86, 			// 
		AIRCRAFT_AUTOPILOT_AVAILABLE = 87, 			// 
		AIRCRAFT_FLAPS_AVAILABLE = 88, 			// 
		AIRCRAFT_STALL_HORN_AVAILABLE = 89, 			// 
		AIRCRAFT_ENGINE_MIXTURE_AVAILABLE = 90, 			// 
		AIRCRAFT_CARB_HEAT_AVAILABLE = 91, 			// 
		AIRCRAFT_SPOILER_AVAILABLE = 92, 			// 
		AIRCRAFT_IS_TAIL_DRAGGER = 93, 			// 
		AIRCRAFT_STROBES_AVAILABLE = 94, 			// 
		AIRCRAFT_PROP_TYPE_AVAILABLE = 95, 			// PROP_TYPE_FIXED, PROP_TYPE_MANUAL, PROP_TYPE_AUTOMATIC
		AIRCRAFT_TOE_BRAKES_AVAILABLE = 96, 			// 
		AIRCRAFT_NAV1_AVAILABLE = 97, 			// 
		AIRCRAFT_NAV2_AVAILABLE = 98, 			// 
		AIRCRAFT_NAV1_OBS_AVAILABLE = 99, 			// 
		AIRCRAFT_NAV2_OBS_AVAILABLE = 100, 			// 
		AIRCRAFT_VOR2_GAUGE_AVAILABLE = 101, 			// 
		AIRCRAFT_GYRO_DRIFT_AVAILABLE = 102, 			// 
		AUTOPILOT_ACTIVE = 103, 			// autopilot master on/off
		AUTOPILOT_WING_LEVELER = 104, 			// wing leveler on/off
		AUTOPILOT_NAV1_LOCK = 105, 			// nav 1 lock on/off
		AUTOPILOT_HEADING_LOCK = 106, 			// heading lock on/off
		AUTOPILOT_HEADING_LOCK_DIR = 107, 			// heading lock direction
		AUTOPILOT_ALTITUDE_LOCK = 108, 			// altitude lock on/off
		AUTOPILOT_ALTITUDE_LOCK_VAR = 109, 			// altitude lock altitude in appropriate units
		AUTOPILOT_ATTITUDE_HOLD = 110, 			// attitude hold
		AUTOPILOT_GLIDESLOPE_HOLD = 111, 			// glideslope hold
		AUTOPILOT_APROACH_HOLD = 112, 			// approach hold
		AUTOPILOT_BACKCOURSE_HOLD = 113, 			// backcourse hold
		AUTOPILOT_YAW_DAMPER = 114, 			// yaw damper
		AUTOPILOT_AIRSPEED_HOLD = 115, 			// airspeed hold active
		AUTOPILOT_AIRSPEED_HOLD_VAR = 116, 			// airspeed hold var
		AUTOPILOT_MACH_HOLD = 117, 			// airspeed hold
		AUTOPILOT_MACH_HOLD_VAR = 118, 			// airspeed hold var
		AUTOPILOT_VERTICAL_HOLD = 119, 			// vertical hold
		AUTOPILOT_VERTICAL_HOLD_VAR = 120, 			// vertical speed hold var in appropriate units
		AUTOPILOT_ALTITUDE_MANUALLY_TUNABLE = 121, 			// 
		AUTOPILOT_HEADING_MANUALLY_TUNABLE = 122, 			// 
		AUTOPILOT_AUTO_THROTTLE_ARM = 123, 			// 
		AUTOPILOT_TAKEOFF_POWER_ACTIVE = 124, 			// 
		ENGINE_RPM_HOLD = 125, 			// 
		ENGINE_CONTROL_SELECT = 126, 			// which engine is being controlled
		ENGINE1 = 127, 			// 
		ENGINE1_THROTTLE_LEVER_POS = 128, 			// throttle position           (16k = 100% throttle position)
		ENGINE1_PROPELLER_LEVER_POS = 129, 			// propeller lever position    (16k = 100% prop setting)
		ENGINE1_MIXTURE_LEVER_POS = 130, 			// mixture position            (16k = 100% mixture position)
		ENGINE1_COMBUSTION = 131, 			// combustion flag             (combustion = TRUE)
		ENGINE1_STARTER_SWITCH_POS = 132, 			// starter switch position
		ENGINE1_MAGNETO_LEFT = 133, 			// left magneto status         (activated = 1)
		ENGINE1_MAGNETO_RIGHT = 134, 			// right magneto status        (activated = 1)
		ENGINE1_N1_RPM = 135, 			// gas producer (RPM)          (16k = 100% RPM)
		ENGINE1_N2_RPM = 136, 			// power (RPM)                 (16k = 100% RPM)
		ENGINE1_FF_GPH = 137, 			// 
		ENGINE1_FF_PPH = 138, 			// fuel flow PPH
		ENGINE1_FF_PPH_SSL = 139, 			// fuel flow (PPH)                  (16k = 100%, Standard sea level conditions)
		ENGINE1_TORQUE = 140, 			// torque produced ( FT*LBS )
		ENGINE1_ANTI_ICE_SWITCH = 141, 			// anti ice switch
		ENGINE1_PRESSURE_RATIO = 142, 			// engine pressure ratio            (65k = 6.4)
		ENGINE1_PRESSURE_BUG_POS = 143, 			// epr governed engine setting      (16k = 100% epr setting)
		ENGINE1_EGT = 144, 			// exhaust gas temp (degC)          (16k = 860 deg)
		ENGINE1_EGT_BUG_POS = 145, 			// egt governed engine setting      (16k = 100% egt setting)
		ENGINE1_CHT = 146, 			// cylinder head temperature
		ENGINE1_OIL_TEMPERATURE = 147, 			// oil temperature (degC)           (16k = 140 deg)
		ENGINE1_OIL_PRESSURE = 148, 			// oil pressure (PSI)               (16k = 55 PSI)
		ENGINE1_OIL_QUANTITY = 149, 			// oil quantity                     (16k = 100%)
		ENGINE1_HYDRAULIC_PRESSURE = 150, 			// hydraulic pressure (PSI)         (16k = 4 psi)
		ENGINE1_HYDRAULIC_QUANTITY = 151, 			// hydraulic quantity               (16k = 100%)
		ENGINE1_MANIFOLD_PRESSURE = 152, 			// manifold pressure                (65k = 64 inhg)
		ENGINE1_ENGINE_VIBRATION = 153, 			// oil quantity                     (16k = 5)
		ENGINE1_RPM_SCALER = 154, 			// *16384 gives actual prop rpm in dx (= 65535 for jets)
		ENGINE1_TURBINE_TEMPERATURE = 155, 			// (DegC * 16384)
		ENGINE1_TORQUE_PERCENT = 156, 			// (FT/LBS * 16384)
		ENGINE1_FUEL_PRESSURE = 157, 			// (PSI * 16384)
		ENGINE1_ELECTRICAL_LOAD = 158, 			// (Max Load * 16384)
		ENGINE1_TRANSMISSION_PRESSURE = 159, 			// (PSI * 16384)
		ENGINE1_TRANSMISSION_TEMPERATURE = 160, 			// (DegC * 16384)
		ENGINE1_ROTOR_RPM = 161, 			// (Max 8 16384)
		ENGINE2 = 162, 			// 
		ENGINE2_THROTTLE_LEVER_POS = 163, 			// throttle position            (0-16384=>0-100%throttle position)
		ENGINE2_PROPELLER_LEVER_POS = 164, 			// propeller lever position     (0-16384=>0-100%prop setting)
		ENGINE2_MIXTURE_LEVER_POS = 165, 			// mixture position             (0-16384=>0-100%mixture position)
		ENGINE2_COMBUSTION = 166, 			// combustion flag              (combustion = TRUE)
		ENGINE2_STARTER_SWITCH_POS = 167, 			// starter switch position
		ENGINE2_MAGNETO_LEFT = 168, 			// left magneto status          (activated = 1)
		ENGINE2_MAGNETO_RIGHT = 169, 			// right magneto status         (activated = 1)
		ENGINE2_N1_RPM = 170, 			// gas producer RPM             (0-16384=>0-100%RPM)
		ENGINE2_N2_RPM = 171, 			// power RPM                    (0-16384=>0-100%RPM)
		ENGINE2_FF_GPH = 172, 			// 
		ENGINE2_FF_PPH = 173, 			// PPH
		ENGINE2_FF_PPH_SSL = 174, 			// fuel flow (0-16384=>0-100%)PPH, Standard sea level conditions
		ENGINE2_TORQUE = 175, 			// torque produced ( FT*LBS )
		ENGINE2_ANTI_ICE_SWITCH = 176, 			// anti ice switch
		ENGINE2_PRESSURE_RATIO = 177, 			// engine pressure ratio        (0-65536=>0-6.4)
		ENGINE2_PRESSURE_BUG_POS = 178, 			// epr governed engine setting  (0-16384=>0-100%epr setting)
		ENGINE2_EGT = 179, 			// exhaust gas temperature      (degC)
		ENGINE2_EGT_BUG_POS = 180, 			// egt governed engine setting  (0-16384=>0-100%egt setting)
		ENGINE2_CHT = 181, 			// cylinder head temperature
		ENGINE2_OIL_TEMPERATURE = 182, 			// oil temperature              (degC)
		ENGINE2_OIL_PRESSURE = 183, 			// oil pressure                 (PSI)
		ENGINE2_OIL_QUANTITY = 184, 			// oil quantity                 (0-16384 => 0-100%)
		ENGINE2_HYDRAULIC_PRESSURE = 185, 			// hydraulic pressure           (0-16384 => 0-4 psi)
		ENGINE2_HYDRAULIC_QUANTITY = 186, 			// hydraulic quantity           (0-16384 => 0-100%)
		ENGINE2_MANIFOLD_PRESSURE = 187, 			// manifold pressure            (0-65536=>0-64)
		ENGINE2_ENGINE_VIBRATION = 188, 			// oil quantity                 (0-16384 => 0-5)
		ENGINE2_RPM_SCALER = 189, 			// *16384 gives actual prop rpm in dx (= 65535 for jets)
		ENGINE2_TURBINE_TEMPERATURE = 190, 			// (DegC * 16384)
		ENGINE2_TORQUE_PERCENT = 191, 			// (FT/LBS * 16384)
		ENGINE2_FUEL_PRESSURE = 192, 			// (PSI * 16384)
		ENGINE2_ELECTRICAL_LOAD = 193, 			// (Max Load * 16384)
		ENGINE2_TRANSMISSION_PRESSURE = 194, 			// (PSI * 16384)
		ENGINE2_TRANSMISSION_TEMPERATURE = 195, 			// (DegC * 16384)
		ENGINE2_ROTOR_RPM = 196, 			// (Max 8 16384)
		ENGINE3 = 197, 			// 
		ENGINE3_THROTTLE_LEVER_POS = 198, 			// throttle position            (0-16384=>0-100%throttle position)
		ENGINE3_PROPELLER_LEVER_POS = 199, 			// propeller lever position     (0-16384=>0-100%prop setting)
		ENGINE3_MIXTURE_LEVER_POS = 200, 			// mixture position             (0-16384=>0-100%mixture position)
		ENGINE3_COMBUSTION = 201, 			// combustion flag              (combustion = TRUE)
		ENGINE3_STARTER_SWITCH_POS = 202, 			// starter switch position
		ENGINE3_MAGNETO_LEFT = 203, 			// left magneto status          (activated = 1)
		ENGINE3_MAGNETO_RIGHT = 204, 			// right magneto status         (activated = 1)
		ENGINE3_N1_RPM = 205, 			// gas producer RPM             (0-16384=>0-100%RPM)
		ENGINE3_N2_RPM = 206, 			// power RPM                    (0-16384=>0-100%RPM)
		ENGINE3_FF_GPH = 207, 			// 
		ENGINE3_FF_PPH = 208, 			// fuel flow PPH
		ENGINE3_FF_PPH_SSL = 209, 			// fuel flow                    (0-16384=>0-100%)PPH, Standard sea level conditions
		ENGINE3_TORQUE = 210, 			// torque produced              ( FT*LBS )
		ENGINE3_ANTI_ICE_SWITCH = 211, 			// anti ice switch
		ENGINE3_PRESSURE_RATIO = 212, 			// engine pressure ratio        (0-65536=>0-6.4)
		ENGINE3_PRESSURE_BUG_POS = 213, 			// epr governed engine setting  (0-16384=>0-100%epr setting)
		ENGINE3_EGT = 214, 			// exhaust gas temperature      (degC)
		ENGINE3_EGT_BUG_POS = 215, 			// egt governed engine setting  (0-16384=>0-100%egt setting)
		ENGINE3_CHT = 216, 			// cylinder head temperature
		ENGINE3_OIL_TEMPERATURE = 217, 			// oil temperature              (degC)
		ENGINE3_OIL_PRESSURE = 218, 			// oil pressure                 (PSI)
		ENGINE3_OIL_QUANTITY = 219, 			// oil quantity                 (0-16384 => 0-100%)
		ENGINE3_HYDRAULIC_PRESSURE = 220, 			// hydraulic pressure           (0-16384 => 0-4 psi)
		ENGINE3_HYDRAULIC_QUANTITY = 221, 			// hydraulic quantity           (0-16384 => 0-100%)
		ENGINE3_MANIFOLD_PRESSURE = 222, 			// manifold pressure            (0-65536=>0-64)
		ENGINE3_ENGINE_VIBRATION = 223, 			// oil quantity                 (0-16384 => 0-5)
		ENGINE3_RPM_SCALER = 224, 			// *16384 gives actual prop rpm in dx (= 65535 for jets)
		ENGINE3_TURBINE_TEMPERATURE = 225, 			// (DegC * 16384)
		ENGINE3_TORQUE_PERCENT = 226, 			// (FT/LBS * 16384)
		ENGINE3_FUEL_PRESSURE = 227, 			// (PSI * 16384)
		ENGINE3_ELECTRICAL_LOAD = 228, 			// (Max Load * 16384)
		ENGINE3_TRANSMISSION_PRESSURE = 229, 			// (PSI * 16384)
		ENGINE3_TRANSMISSION_TEMPERATURE = 230, 			// (DegC * 16384)
		ENGINE3_ROTOR_RPM = 231, 			// (Max 8 16384)
		ENGINE4 = 232, 			// 
		ENGINE4_THROTTLE_LEVER_POS = 233, 			// throttle position            (0-16384=>0-100%throttle position)
		ENGINE4_PROPELLER_LEVER_POS = 234, 			// propeller lever position     (0-16384=>0-100%prop setting)
		ENGINE4_MIXTURE_LEVER_POS = 235, 			// mixture position             (0-16384=>0-100%mixture position)
		ENGINE4_COMBUSTION = 236, 			// combustion flag              (combustion = TRUE)
		ENGINE4_STARTER_SWITCH_POS = 237, 			// starter switch position
		ENGINE4_MAGNETO_LEFT = 238, 			// left magneto status          (activated = 1)
		ENGINE4_MAGNETO_RIGHT = 239, 			// right magneto status         (activated = 1)
		ENGINE4_N1_RPM = 240, 			// gas producer RPM             (0-16384=>0-100%RPM)
		ENGINE4_N2_RPM = 241, 			// power RPM                    (0-16384=>0-100%RPM)
		ENGINE4_FF_GPH = 242, 			// 
		ENGINE4_FF_PPH = 243, 			// fuel flow PPH
		ENGINE4_FF_PPH_SSL = 244, 			// fuel flow                    (0-16384=>0-100%)PPH, Standard sea level conditions
		ENGINE4_TORQUE = 245, 			// torque produced              ( FT*LBS )
		ENGINE4_ANTI_ICE_SWITCH = 246, 			// anti ice switch
		ENGINE4_PRESSURE_RATIO = 247, 			// engine pressure ratio        (0-65536=>0-6.4)
		ENGINE4_PRESSURE_BUG_POS = 248, 			// epr governed engine setting  (0-16384=>0-100%epr setting)
		ENGINE4_EGT = 249, 			// exhaust gas temperature      (degC)
		ENGINE4_EGT_BUG_POS = 250, 			// egt governed engine setting  (0-16384=>0-100%egt setting)
		ENGINE4_CHT = 251, 			// cylinder head temperature
		ENGINE4_OIL_TEMPERATURE = 252, 			// oil temperature              (degC)
		ENGINE4_OIL_PRESSURE = 253, 			// oil pressure                 (PSI)
		ENGINE4_OIL_QUANTITY = 254, 			// oil quantity (0-16384 => 0-100%)
		ENGINE4_HYDRAULIC_PRESSURE = 255, 			// hydraulic pressure (0-16384 => 0-4 psi)
		ENGINE4_HYDRAULIC_QUANTITY = 256, 			// hydraulic quantity (0-16384 => 0-100%)
		ENGINE4_MANIFOLD_PRESSURE = 257, 			// manifold pressure (0-65536=>0-64)
		ENGINE4_ENGINE_VIBRATION = 258, 			// oil quantity (0-16384 => 0-5)
		ENGINE4_RPM_SCALER = 259, 			// *16384 gives actual prop rpm in dx (= 65535 for jets)
		ENGINE4_TURBINE_TEMPERATURE = 260, 			// (DegC * 16384)
		ENGINE4_TORQUE_PERCENT = 261, 			// (FT/LBS * 16384)
		ENGINE4_FUEL_PRESSURE = 262, 			// (PSI * 16384)
		ENGINE4_ELECTRICAL_LOAD = 263, 			// (Max Load * 16384)
		ENGINE4_TRANSMISSION_PRESSURE = 264, 			// (PSI * 16384)
		ENGINE4_TRANSMISSION_TEMPERATURE = 265, 			// (DegC * 16384)
		ENGINE4_ROTOR_RPM = 266, 			// (Max 8 16384)
		NUMBER_OF_ENGINES = 267, 			// 1=single 2=twin ...
		PROP_ADVANCE_SELECTION = 268, 			// 0=fixed 1=optimal 2=manual
		THROTTLE_LOWER_LIMIT = 269, 			// set by sim for thrust reversers
		OIL_QUANTITY_TOTAL = 270, 			// 0..7fff (8 qt.)
		SYSTEMS_AVAILABLE = 271, 			// 
		INSTRUMENTS_AVAILABLE = 272, 			// 
		PP_ADF = 273, 			// partial panel variables
		PP_AIR = 274, 			// 
		PP_ALTIM = 275, 			// 
		PP_ATTITUDE = 276, 			// 
		PP_COM = 277, 			// 
		PP_COMPASS = 278, 			// 
		PP_ELECT = 279, 			// there is no electricity in panels circuit
		PP_ENGINE = 280, 			// 
		PP_FUEL_IND = 281, 			// 
		PP_HEADING = 282, 			// 
		PP_VERT_VEL = 283, 			// 
		PP_XPNDR = 284, 			// 
		PP_NAV = 285, 			// 
		PP_PITOT = 286, 			// 
		PP_TURN_COORD = 287, 			// 
		PP_VACUUM = 288, 			// 
		FUEL_TANK_CENTER_LEVEL = 289, 			// 
		FUEL_TANK_CENTER_CAPACITY = 290, 			// 
		FUEL_TANK_LEFT_MAIN_LEVEL = 291, 			// 
		FUEL_TANK_LEFT_MAIN_CAPACITY = 292, 			// 
		FUEL_TANK_LEFT_AUX_LEVEL = 293, 			// 
		FUEL_TANK_LEFT_AUX_CAPACITY = 294, 			// 
		FUEL_TANK_LEFT_TIP_LEVEL = 295, 			// 
		FUEL_TANK_LEFT_TIP_CAPACITY = 296, 			// 
		FUEL_TANK_RIGHT_MAIN_LEVEL = 297, 			// 
		FUEL_TANK_RIGHT_MAIN_CAPACITY = 298, 			// 
		FUEL_TANK_RIGHT_AUX_LEVEL = 299, 			// 
		FUEL_TANK_RIGHT_AUX_CAPACITY = 300, 			// 
		FUEL_TANK_RIGHT_TIP_LEVEL = 301, 			// 
		FUEL_TANK_RIGHT_TIP_CAPACITY = 302, 			// 
		FUEL_QUANTITY_RIGHT = 303, 			// Total Gallons
		FUEL_QUANTITY_LEFT = 304, 			// Total Gallons
		FUEL_QUANTITY_CENTER = 305, 			// Total Gallons
		FUEL_WEGHT_PER_GALLON = 306, 			// in pounds I.F  (6 lbs/gal = 1536)
		FUEL_TANK_SELECTOR = 307, 			// see FUEL_TANK_SELECTOR_ enum values
		CROSS_FEED_SELECTOR = 308, 			// OFF, LEFT_TO_RIGHT, or RIGHT_TO_LEFT
		YOKE_POS_Y = 309, 			// (OBSOLETE)
		YOKE_IND_Y = 310, 			// (OBSOLETE)
		YOKE_POS_X = 311, 			// (OBSOLETE)
		YOKE_IND_X = 312, 			// (OBSOLETE)
		AILERON_POS = 313, 			// (OBSOLETE)
		RUDDER_PEDAL_POS = 314, 			// (OBSOLETE)
		RUDDER_PEDAL_IND = 315, 			// (OBSOLETE)
		RUDDER_POS = 316, 			// (OBSOLETE)
		ELEVATOR_POS = 317, 			// (OBSOLETE)
		ELEVATOR_TRIM_POS = 318, 			// (OBSOLETE)
		ELEVATOR_TRIM_IND = 319, 			// (OBSOLETE)
		BRAKES_LEFT_POS = 320, 			// 0=off 32k=full brakes
		BRAKES_RIGHT_POS = 321, 			// 0=off 32k=full brakes
		BRAKE_IND = 322, 			// 0=off 16k=full brakes
		PARKING_BRAKE_POS = 323, 			// 0=off 32k=full  parking brakes
		SPOILERS_ARMED = 324, 			// 
		SPOILERS_HANDLE_POS = 325, 			// 
		SPOILERS_POS_LEFT = 326, 			// 
		SPOILERS_POS_RIGHT = 327, 			// 
		FLAPS_HANDLE_POS = 328, 			// 
		FLAPS_POS_LEFT = 329, 			// 
		FLAPS_POS_RIGHT = 330, 			// 
		GEAR_HANDLE_POS = 331, 			// 
		GEAR_POS_NOSE = 332, 			// 
		GEAR_POS_LEFT = 333, 			// 
		GEAR_POS_RIGHT = 334, 			// 
		DISPLAY_UNITS = 335, 			// english vs. metric system
		SIM_SPEED = 336, 			// time dilation factor for sim equations
		ZOOM_FACTOR = 337, 			// zoom value of current window in real units
		REALISM_SENSE = 338, 			// 
		REALISM_SYSTEMS = 339, 			// 
		RELIABILITY_SENSE = 340, 			// 
		AMBIENT_TEMP_DEGREES_C = 341, 			// 
		AMBIENT_PRES_MBAR = 342, 			// Millibars * 16
		AMBIENT_WIND_VEL = 343, 			// 
		AMBIENT_WIND_DIR = 344, 			// 
		AMBIENT_WINDE = 345, 			// MPS x(east) wind component
		AMBIENT_WINDN = 346, 			// MPS z(up) wind component
		AMBIENT_WINDA = 347, 			// MPS y(north) wind component
		BAROMETRIC_PRESSURE = 348, 			// barometric pressure experienced
		ANGLE_OF_ATTACK_INDICATOR = 349, 			// 
		TOTAL_AIR_TEMP = 350, 			// 
		G_FORCE = 351, 			// 
		CHARGING_AMPS = 352, 			// charging system amps - 1 amp=65535
		SUCTION_PRESSURE = 353, 			// in hg
		MARKER_BEACON_STATE = 354, 			// 0=no beacon, 1=outer, 2=middle, 3=inner
		CURRENT_DME = 355, 			// DMECUR_DME1 or DMECUR_DME2
		PANEL_STATE_FLAGS = 356, 			// 
		GET_GUN_AMMO = 357, 			// 
		GET_CANNON_AMMO = 358, 			// 
		GET_ROCKET_AMMO = 359, 			// 
		GET_BOMB_AMMO = 360, 			// 
		ENGINE_PRIMER = 361, 			// 0.0 -> 1.0 == in -> out
		COWL_FLAPS = 362, 			// 0.0 -> 1.0 == closed -> open
		MASTER_ALTERNATOR = 363, 			// 0.0 -> 1.0 == off -> on
		MASTER_BATTERY = 364, 			// 0.0 -> 1.0 == off -> on
		CARB_HEAT = 365, 			// 0.0 -> 1.0 == off -> on
		FLIGHT_TIME = 366, 			// in 1/10 of hours
		FUEL_PUMP = 367, 			// 0.0 -> 1.0 == off -> on
		BEACON_LIGHTS = 368, 			// 0.0 -> 1.0 == off -> on
		TAXI_LIGHTS = 369, 			// 0.0 -> 1.0 == off -> on
		RADIO_STACK_VISIBLE = 370, 			// 0.0 -> 1.0 == off -> on
		HORIZON_BAR_OFFSET = 371, 			// -10 deg to + 10 deg
		CURRENT_DME_DISTANCE = 372, 			// 
		CURRENT_DME_SPEED = 373, 			// 
		CURRENT_DME_MIN = 374, 			// 
		WARNING_FUEL = 375, 			// 
		WARNING_FUEL_L = 376, 			// 
		WARNING_FUEL_R = 377, 			// 
		WARNING_VACUUM = 378, 			// 
		WARNING_VACUUM_L = 379, 			// 
		WARNING_VACUUM_R = 380, 			// 
		WARNING_OIL_PSI = 381, 			// 
		WARNING_VOLTAGE = 382, 			// 
		FUEL_TANK_CENTER2_LEVEL = 383, 			// 
		FUEL_TANK_CENTER2_CAPACITY = 384, 			// 
		FUEL_QUANTITY_CENTER2 = 385, 			// Total Gallons
		TOTAL_FUEL_QUANTITY_GALLONS = 386, 			// 
		SELECTED_FUEL_QUANTITY_GALLONS = 387, 			// 
		SELECTED_FUEL_QUANTITY_LITERS = 388, 			// 
		SELECTED_FUEL_QUANTITY_PERCENT = 389, 			// 
		RECIP_ENGINE1_ENGINE_RPM = 390, 			// 
		RECIP_ENGINE1_MANIFOLD_PRESSURE = 391, 			// 
		RECIP_ENGINE1_THROTTLE_LEVER_POS = 392, 			// 
		RECIP_ENGINE1_MIXTURE_LEVER_POS = 393, 			// 
		RECIP_ENGINE1_PROP_LEVER_POS = 394, 			// 
		RECIP_ENGINE1_COWL_FLAP_POS = 395, 			// 0 = closed, 1 = open
		RECIP_ENGINE1_CARB_HEAT_POS = 396, 			// 0 = off, 1 = on
		RECIP_ENGINE1_ALTERNATE_AIR_POS = 397, 			// 0 = closed, 1 = off
		RECIP_ENGINE1_COOLANT_RESERVOIR_PCT = 398, 			// 
		RECIP_ENGINE1_LEFT_MAG = 399, 			// 
		RECIP_ENGINE1_RIGHT_MAG = 400, 			// 
		RECIP_ENGINE1_STARTER = 401, 			// 
		RECIP_ENGINE1_BRAKE_POWER = 402, 			// power delivered to the shaft
		RECIP_ENGINE1_SHAFT_TORQUE = 403, 			// torque delivered to the shaft
		RECIP_ENGINE1_PROP_TORQUE = 404, 			// torque required by prop
		RECIP_ENGINE1_STARTER_TORQUE = 405, 			// torque applied by starter
		RECIP_ENGINE1_COMBUSTION = 406, 			// combustion flag
		RECIP_ENGINE1_TURBOCHARGER_FAILED = 407, 			// 
		RECIP_ENGINE1_EMERGENCY_BOOST_ACTIVE = 408, 			// like WEP in the P51
		RECIP_ENGINE1_EMERGENCY_BOOST_ELAPSED_TIME = 409, 			// how long emergency has been active
		RECIP_ENGINE1_WASTEGATE_POS = 410, 			// position of wastegate controller 1=closed (full boost), 0=open (no boost)
		RECIP_ENGINE1_PROP_BETA = 411, 			// blade angle (relative to plane of rotation)
		RECIP_ENGINE1_THRUST = 412, 			// lbs
		RECIP_ENGINE1_EGT_DEGR = 413, 			// EGT degrees Rankine
		RECIP_ENGINE1_TIT_DEGR = 414, 			// Turbine Inlet Temperature degrees Rankine
		RECIP_ENGINE1_CHT_DEGR = 415, 			// CHT degrees Rankine
		RECIP_ENGINE1_OIL_TEMP_DEGR = 416, 			// Oil Temperature degrees Rankine
		RECIP_ENGINE1_RADIATOR_DEGR = 417, 			// Radiator temperature
		RECIP_ENGINE1_OIL_PRESS_PSF = 418, 			// Oil Pressure lbs/sqft
		RECIP_ENGINE1_OIL_LEAK_PCT = 419, 			// Percent Oil leaked
		RECIP_ENGINE1_FUEL_AVAILABLE = 420, 			// Fuel available to engine?
		RECIP_ENGINE1_FUELFLOW_PPH = 421, 			// Fuel flow in lbs/hr
		RECIP_ENGINE1_TANK_SELECTOR = 422, 			// Fuel tank selector
		RECIP_ENGINE1_TANKS_USED = 423, 			// Fuel tanks used
		RECIP_ENGINE1_NUMBER_OF_TANKS_USED = 424, 			// Number of fuel tanks used
		RECIP_ENGINE1_FUEL_PRESSURE_PSF = 425, 			// Fuel pressure in lbs/sqft
		RECIP_ENGINE1_COMBUSTION_SOUND_PCT = 426, 			// Percent of Sound (0 - 1)
		RECIP_ENGINE1_INDUCED_VELOCITY = 427, 			// fps, velocity induced by prop
		RECIP_ENGINE1_DAMAGE_PERCENT = 428, 			// 0-100%
		RECIP_ENGINE2_ENGINE_RPM = 429, 			// 
		RECIP_ENGINE2_MANIFOLD_PRESSURE = 430, 			// 
		RECIP_ENGINE2_THROTTLE_LEVER_POS = 431, 			// 
		RECIP_ENGINE2_MIXTURE_LEVER_POS = 432, 			// 
		RECIP_ENGINE2_PROP_LEVER_POS = 433, 			// 
		RECIP_ENGINE2_COWL_FLAP_POS = 434, 			// 0 = closed, 1 = open
		RECIP_ENGINE2_CARB_HEAT_POS = 435, 			// 0 = off, 1 = on
		RECIP_ENGINE2_ALTERNATE_AIR_POS = 436, 			// 0 = closed, 1 = off
		RECIP_ENGINE2_COOLANT_RESERVOIR_PCT = 437, 			// 
		RECIP_ENGINE2_LEFT_MAG = 438, 			// 
		RECIP_ENGINE2_RIGHT_MAG = 439, 			// 
		RECIP_ENGINE2_STARTER = 440, 			// 
		RECIP_ENGINE2_BRAKE_POWER = 441, 			// power delivered to the shaft
		RECIP_ENGINE2_SHAFT_TORQUE = 442, 			// torque delivered to the shaft
		RECIP_ENGINE2_PROP_TORQUE = 443, 			// torque required by prop
		RECIP_ENGINE2_STARTER_TORQUE = 444, 			// torque applied by starter
		RECIP_ENGINE2_COMBUSTION = 445, 			// combustion flag
		RECIP_ENGINE2_TURBOCHARGER_FAILED = 446, 			// 
		RECIP_ENGINE2_EMERGENCY_BOOST_ACTIVE = 447, 			// like WEP in the P51
		RECIP_ENGINE2_EMERGENCY_BOOST_ELAPSED_TIME = 448, 			// how long emergency has been active
		RECIP_ENGINE2_WASTEGATE_POS = 449, 			// position of wastegate controller 1=closed (full boost), 0=open (no boost)
		RECIP_ENGINE2_PROP_BETA = 450, 			// blade angle (relative to plane of rotation)
		RECIP_ENGINE2_THRUST = 451, 			// lbs
		RECIP_ENGINE2_EGT_DEGR = 452, 			// EGT degrees Rankine
		RECIP_ENGINE2_TIT_DEGR = 453, 			// Turbine Inlet Temperature degrees Rankine
		RECIP_ENGINE2_CHT_DEGR = 454, 			// CHT degrees Rankine
		RECIP_ENGINE2_OIL_TEMP_DEGR = 455, 			// Oil Temperature degrees Rankine
		RECIP_ENGINE2_RADIATOR_DEGR = 456, 			// Radiator temperature
		RECIP_ENGINE2_OIL_PRESS_PSF = 457, 			// Oil Pressure lbs/sqft
		RECIP_ENGINE2_OIL_LEAK_PCT = 458, 			// Percent Oil leaked
		RECIP_ENGINE2_FUEL_AVAILABLE = 459, 			// Fuel available to engine?
		RECIP_ENGINE2_FUELFLOW_PPH = 460, 			// Fuel flow in lbs/hr
		RECIP_ENGINE2_TANK_SELECTOR = 461, 			// Fuel tank selector
		RECIP_ENGINE2_TANKS_USED = 462, 			// Fuel tanks used
		RECIP_ENGINE2_NUMBER_OF_TANKS_USED = 463, 			// Number of fuel tanks used
		RECIP_ENGINE2_FUEL_PRESSURE_PSF = 464, 			// Fuel pressure in lbs/sqft
		RECIP_ENGINE2_COMBUSTION_SOUND_PCT = 465, 			// Percent of Sound (0 - 1)
		RECIP_ENGINE2_INDUCED_VELOCITY = 466, 			// fps, velocity induced by prop
		RECIP_ENGINE2_DAMAGE_PERCENT = 467, 			// 0-100%
		RECIP_ENGINE3_ENGINE_RPM = 468, 			// 
		RECIP_ENGINE3_MANIFOLD_PRESSURE = 469, 			// 
		RECIP_ENGINE3_THROTTLE_LEVER_POS = 470, 			// 
		RECIP_ENGINE3_MIXTURE_LEVER_POS = 471, 			// 
		RECIP_ENGINE3_PROP_LEVER_POS = 472, 			// 
		RECIP_ENGINE3_COWL_FLAP_POS = 473, 			// 0 = closed, 1 = open
		RECIP_ENGINE3_CARB_HEAT_POS = 474, 			// 0 = off, 1 = on
		RECIP_ENGINE3_ALTERNATE_AIR_POS = 475, 			// 0 = closed, 1 = off
		RECIP_ENGINE3_COOLANT_RESERVOIR_PCT = 476, 			// 
		RECIP_ENGINE3_LEFT_MAG = 477, 			// 
		RECIP_ENGINE3_RIGHT_MAG = 478, 			// 
		RECIP_ENGINE3_STARTER = 479, 			// 
		RECIP_ENGINE3_BRAKE_POWER = 480, 			// power delivered to the shaft
		RECIP_ENGINE3_SHAFT_TORQUE = 481, 			// torque delivered to the shaft
		RECIP_ENGINE3_PROP_TORQUE = 482, 			// torque required by prop
		RECIP_ENGINE3_STARTER_TORQUE = 483, 			// torque applied by starter
		RECIP_ENGINE3_COMBUSTION = 484, 			// combustion flag
		RECIP_ENGINE3_TURBOCHARGER_FAILED = 485, 			// 
		RECIP_ENGINE3_EMERGENCY_BOOST_ACTIVE = 486, 			// like WEP in the P51
		RECIP_ENGINE3_EMERGENCY_BOOST_ELAPSED_TIME = 487, 			// how long emergency has been active
		RECIP_ENGINE3_WASTEGATE_POS = 488, 			// position of wastegate controller 1=closed (full boost), 0=open (no boost)
		RECIP_ENGINE3_PROP_BETA = 489, 			// blade angle (relative to plane of rotation)
		RECIP_ENGINE3_THRUST = 490, 			// lbs
		RECIP_ENGINE3_EGT_DEGR = 491, 			// EGT degrees Rankine
		RECIP_ENGINE3_TIT_DEGR = 492, 			// Turbine Inlet Temperature degrees Rankine
		RECIP_ENGINE3_CHT_DEGR = 493, 			// CHT degrees Rankine
		RECIP_ENGINE3_OIL_TEMP_DEGR = 494, 			// Oil Temperature degrees Rankine
		RECIP_ENGINE3_RADIATOR_DEGR = 495, 			// Radiator temperature
		RECIP_ENGINE3_OIL_PRESS_PSF = 496, 			// Oil Pressure lbs/sqft
		RECIP_ENGINE3_OIL_LEAK_PCT = 497, 			// Percent Oil leaked
		RECIP_ENGINE3_FUEL_AVAILABLE = 498, 			// Fuel available to engine?
		RECIP_ENGINE3_FUELFLOW_PPH = 499, 			// Fuel flow in lbs/hr
		RECIP_ENGINE3_TANK_SELECTOR = 500, 			// Fuel tank selector
		RECIP_ENGINE3_TANKS_USED = 501, 			// Fuel tanks used
		RECIP_ENGINE3_NUMBER_OF_TANKS_USED = 502, 			// Number of fuel tanks used
		RECIP_ENGINE3_FUEL_PRESSURE_PSF = 503, 			// Fuel pressure in lbs/sqft
		RECIP_ENGINE3_COMBUSTION_SOUND_PCT = 504, 			// Percent of Sound (0 - 1)
		RECIP_ENGINE3_INDUCED_VELOCITY = 505, 			// fps, velocity induced by prop
		RECIP_ENGINE3_DAMAGE_PERCENT = 506, 			// 0-100%
		RECIP_ENGINE4_ENGINE_RPM = 507, 			// 
		RECIP_ENGINE4_MANIFOLD_PRESSURE = 508, 			// 
		RECIP_ENGINE4_THROTTLE_LEVER_POS = 509, 			// 
		RECIP_ENGINE4_MIXTURE_LEVER_POS = 510, 			// 
		RECIP_ENGINE4_PROP_LEVER_POS = 511, 			// 
		RECIP_ENGINE4_COWL_FLAP_POS = 512, 			// 0 = closed, 1 = open
		RECIP_ENGINE4_CARB_HEAT_POS = 513, 			// 0 = off, 1 = on
		RECIP_ENGINE4_ALTERNATE_AIR_POS = 514, 			// 0 = closed, 1 = off
		RECIP_ENGINE4_COOLANT_RESERVOIR_PCT = 515, 			// 
		RECIP_ENGINE4_LEFT_MAG = 516, 			// 
		RECIP_ENGINE4_RIGHT_MAG = 517, 			// 
		RECIP_ENGINE4_STARTER = 518, 			// 
		RECIP_ENGINE4_BRAKE_POWER = 519, 			// power delivered to the shaft
		RECIP_ENGINE4_SHAFT_TORQUE = 520, 			// torque delivered to the shaft
		RECIP_ENGINE4_PROP_TORQUE = 521, 			// torque required by prop
		RECIP_ENGINE4_STARTER_TORQUE = 522, 			// torque applied by starter
		RECIP_ENGINE4_COMBUSTION = 523, 			// combustion flag
		RECIP_ENGINE4_TURBOCHARGER_FAILED = 524, 			// 
		RECIP_ENGINE4_EMERGENCY_BOOST_ACTIVE = 525, 			// like WEP in the P51
		RECIP_ENGINE4_EMERGENCY_BOOST_ELAPSED_TIME = 526, 			// how long emergency has been active
		RECIP_ENGINE4_WASTEGATE_POS = 527, 			// position of wastegate controller 1=closed (full boost), 0=open (no boost)
		RECIP_ENGINE4_PROP_BETA = 528, 			// blade angle (relative to plane of rotation)
		RECIP_ENGINE4_THRUST = 529, 			// lbs
		RECIP_ENGINE4_EGT_DEGR = 530, 			// EGT degrees Rankine
		RECIP_ENGINE4_TIT_DEGR = 531, 			// Turbine Inlet Temperature degrees Rankine
		RECIP_ENGINE4_CHT_DEGR = 532, 			// CHT degrees Rankine
		RECIP_ENGINE4_OIL_TEMP_DEGR = 533, 			// Oil Temperature degrees Rankine
		RECIP_ENGINE4_RADIATOR_DEGR = 534, 			// Radiator temperature
		RECIP_ENGINE4_OIL_PRESS_PSF = 535, 			// Oil Pressure lbs/sqft
		RECIP_ENGINE4_OIL_LEAK_PCT = 536, 			// Percent Oil leaked
		RECIP_ENGINE4_FUEL_AVAILABLE = 537, 			// Fuel available to engine?
		RECIP_ENGINE4_FUELFLOW_PPH = 538, 			// Fuel flow in lbs/hr
		RECIP_ENGINE4_TANK_SELECTOR = 539, 			// Fuel tank selector
		RECIP_ENGINE4_TANKS_USED = 540, 			// Fuel tanks used
		RECIP_ENGINE4_NUMBER_OF_TANKS_USED = 541, 			// Number of fuel tanks used
		RECIP_ENGINE4_FUEL_PRESSURE_PSF = 542, 			// Fuel pressure in lbs/sqft
		RECIP_ENGINE4_COMBUSTION_SOUND_PCT = 543, 			// Percent of Sound (0 - 1)
		RECIP_ENGINE4_INDUCED_VELOCITY = 544, 			// fps, velocity induced by prop
		RECIP_ENGINE4_DAMAGE_PERCENT = 545, 			// 0-100%
		FUEL_QUANTITY_CENTER3 = 546, 			// Total Gallons
		SCORE_STRING_QUICK_COMBAT = 547, 			// 
		SCORE_STRING_MULTIPLAYER = 548, 			// 
		IS_MULTIPLAYER_TEAM_COMBAT = 549, 			// 
		VOR1_BEARING_DEGREES = 550, 			// 
		FORWARD_GROUND_VELOCITY = 551, 			// in ft/sec
		SIDE_GROUND_VELOCITY = 552, 			// in ft/sec
		VERTICAL_GROUND_VELOCITY = 553, 			// in ft/sec
		NORTH_VELOCITY = 554, 			// in ft/sec
		EAST_VELOCITY = 555, 			// in ft/sec
		VERTICAL_VELOCITY = 556, 			// in ft/sec
		CLOCK_TOTAL_SECONDS = 557, 			// local clock time, seconds since midnight
		GENERAL_ENGINE1_THROTTLE_LEVER_POS = 558, 			// 
		GENERAL_ENGINE1_PROPELLER_LEVER_POS = 559, 			// 
		GENERAL_ENGINE1_MIXTURE_LEVER_POS = 560, 			// 
		GENERAL_ENGINE1_OIL_TEMP = 561, 			// 
		GENERAL_ENGINE1_OIL_PRES = 562, 			// 
		GENERAL_ENGINE1_STARTER = 563, 			// 
		GENERAL_ENGINE1_FAILURE = 564, 			// 
		GENERAL_ENGINE1_GENERATOR_SWITCH = 565, 			// 
		GENERAL_ENGINE1_GENERATOR_ACTIVE = 566, 			// 
		GENERAL_ENGINE1_EGT = 567, 			// 
		GENERAL_ENGINE2_THROTTLE_LEVER_POS = 568, 			// 
		GENERAL_ENGINE2_PROPELLER_LEVER_POS = 569, 			// 
		GENERAL_ENGINE2_MIXTURE_LEVER_POS = 570, 			// 
		GENERAL_ENGINE2_OIL_TEMP = 571, 			// 
		GENERAL_ENGINE2_OIL_PRES = 572, 			// 
		GENERAL_ENGINE2_STARTER = 573, 			// 
		GENERAL_ENGINE2_FAILURE = 574, 			// 
		GENERAL_ENGINE2_GENERATOR_SWITCH = 575, 			// 
		GENERAL_ENGINE2_GENERATOR_ACTIVE = 576, 			// 
		GENERAL_ENGINE2_EGT = 577, 			// 
		GENERAL_ENGINE3_THROTTLE_LEVER_POS = 578, 			// 
		GENERAL_ENGINE3_PROPELLER_LEVER_POS = 579, 			// 
		GENERAL_ENGINE3_MIXTURE_LEVER_POS = 580, 			// 
		GENERAL_ENGINE3_OIL_TEMP = 581, 			// 
		GENERAL_ENGINE3_OIL_PRES = 582, 			// 
		GENERAL_ENGINE3_STARTER = 583, 			// 
		GENERAL_ENGINE3_FAILURE = 584, 			// 
		GENERAL_ENGINE3_GENERATOR_SWITCH = 585, 			// 
		GENERAL_ENGINE3_GENERATOR_ACTIVE = 586, 			// 
		GENERAL_ENGINE3_EGT = 587, 			// 
		GENERAL_ENGINE4_THROTTLE_LEVER_POS = 588, 			// 
		GENERAL_ENGINE4_PROPELLER_LEVER_POS = 589, 			// 
		GENERAL_ENGINE4_MIXTURE_LEVER_POS = 590, 			// 
		GENERAL_ENGINE4_OIL_TEMP = 591, 			// 
		GENERAL_ENGINE4_OIL_PRES = 592, 			// 
		GENERAL_ENGINE4_STARTER = 593, 			// 
		GENERAL_ENGINE4_FAILURE = 594, 			// 
		GENERAL_ENGINE4_GENERATOR_SWITCH = 595, 			// 
		GENERAL_ENGINE4_GENERATOR_ACTIVE = 596, 			// 
		GENERAL_ENGINE4_EGT = 597, 			// 
		NAV1_COMMNAV_TEST = 598, 			// 
		NAV2_COMMNAV_TEST = 599, 			// 
		COM1_AUTOSWITCH_ON = 600, 			// 
		NAV1_AUTOSWITCH_ON = 601, 			// 
		COM2_AUTOSWITCH_ON = 602, 			// 
		NAV2_AUTOSWITCH_ON = 603, 			// 
		ELAPSED_SECONDS = 604, 			// 
		G_FORCE_MAXIMUM = 605, 			// 
		G_FORCE_MINIMUM = 606, 			// 
		ENGINE_WORKING_TIME = 607, 			// 
		ENGINE1_MAX_RPM = 608, 			// 
		ENGINE2_MAX_RPM = 609, 			// 
		ENGINE3_MAX_RPM = 610, 			// 
		ENGINE4_MAX_RPM = 611, 			// 
		ATTITUDE_BARS_POSITION = 612, 			// 
		RAD_INS_SWITCH = 613, 			// used in concorde only
		RADIO_HEIGHT = 614, 			// meters
		DECISION_HEIGHT = 615, 			// meters
		LOW_HEIGHT_WARNING = 616, 			// boolean
		COM_STBY_FREQUENCY = 617, 			// BCD; same as COM_FREQUENCY
		COM_RADIO_25_KHZ_TUNABLE = 618, 			// Boolean
		NAV_LIGHTS = 619, 			// 
		LOGO_LIGHTS = 620, 			// 
		WING_LIGHTS = 621, 			// 
		RECOGNITION_LIGHTS = 622, 			// 
		RADIO_MORSE_IDENT_STATES = 623, 			// 
		TURB_ENGINE_1_N1 = 624, 			// 
		TURB_ENGINE_1_N2 = 625, 			// 
		TURB_ENGINE_1_CORRECTED_N1 = 626, 			// 
		TURB_ENGINE_1_CORRECTED_N2 = 627, 			// 
		TURB_ENGINE_1_CORRECTED_FF = 628, 			// 
		TURB_ENGINE_1_PCT_MAX_TORQUE = 629, 			// 
		TURB_ENGINE_1_EPR = 630, 			// 
		TURB_ENGINE_1_ITT = 631, 			// 
		TURB_ENGINE_1_AFTERBURNER = 632, 			// 
		TURB_ENGINE_1_JET_THRUST = 633, 			// 
		TURB_ENGINE_1_BLEEDAIRPSI = 634, 			// 
		TURB_ENGINE_1_TANK_SELECTOR = 635, 			// 
		TURB_ENGINE_1_TANKS_USED = 636, 			// 
		TURB_ENGINE_1_NUMBER_TANKS = 637, 			// 
		TURB_ENGINE_1_FUELFLOW_PPH = 638, 			// 
		TURB_ENGINE_1_FUEL_AVAILABLE = 639, 			// 
		TURB_ENGINE_1_PCT_AREA = 640, 			// 
		TURB_ENGINE_1_PCT_REVERSER = 641, 			// 
		TURB_ENGINE_1_VIBRATION = 642, 			// 
		TURB_ENGINE_2_N1 = 643, 			// 
		TURB_ENGINE_2_N2 = 644, 			// 
		TURB_ENGINE_2_CORRECTED_N1 = 645, 			// 
		TURB_ENGINE_2_CORRECTED_N2 = 646, 			// 
		TURB_ENGINE_2_CORRECTED_FF = 647, 			// 
		TURB_ENGINE_2_PCT_MAX_TORQUE = 648, 			// 
		TURB_ENGINE_2_EPR = 649, 			// 
		TURB_ENGINE_2_ITT = 650, 			// 
		TURB_ENGINE_2_AFTERBURNER = 651, 			// 
		TURB_ENGINE_2_JET_THRUST = 652, 			// 
		TURB_ENGINE_2_BLEEDAIRPSI = 653, 			// 
		TURB_ENGINE_2_TANK_SELECTOR = 654, 			// 
		TURB_ENGINE_2_TANKS_USED = 655, 			// 
		TURB_ENGINE_2_NUMBER_TANKS = 656, 			// 
		TURB_ENGINE_2_FUELFLOW_PPH = 657, 			// 
		TURB_ENGINE_2_FUEL_AVAILABLE = 658, 			// 
		TURB_ENGINE_2_PCT_AREA = 659, 			// 
		TURB_ENGINE_2_PCT_REVERSER = 660, 			// 
		TURB_ENGINE_2_VIBRATION = 661, 			// 
		TURB_ENGINE_3_N1 = 662, 			// 
		TURB_ENGINE_3_N2 = 663, 			// 
		TURB_ENGINE_3_CORRECTED_N1 = 664, 			// 
		TURB_ENGINE_3_CORRECTED_N2 = 665, 			// 
		TURB_ENGINE_3_CORRECTED_FF = 666, 			// 
		TURB_ENGINE_3_PCT_MAX_TORQUE = 667, 			// 
		TURB_ENGINE_3_EPR = 668, 			// 
		TURB_ENGINE_3_ITT = 669, 			// 
		TURB_ENGINE_3_AFTERBURNER = 670, 			// 
		TURB_ENGINE_3_JET_THRUST = 671, 			// 
		TURB_ENGINE_3_BLEEDAIRPSI = 672, 			// 
		TURB_ENGINE_3_TANK_SELECTOR = 673, 			// 
		TURB_ENGINE_3_TANKS_USED = 674, 			// 
		TURB_ENGINE_3_NUMBER_TANKS = 675, 			// 
		TURB_ENGINE_3_FUELFLOW_PPH = 676, 			// 
		TURB_ENGINE_3_FUEL_AVAILABLE = 677, 			// 
		TURB_ENGINE_3_PCT_AREA = 678, 			// 
		TURB_ENGINE_3_PCT_REVERSER = 679, 			// 
		TURB_ENGINE_3_VIBRATION = 680, 			// 
		TURB_ENGINE_4_N1 = 681, 			// 
		TURB_ENGINE_4_N2 = 682, 			// 
		TURB_ENGINE_4_CORRECTED_N1 = 683, 			// 
		TURB_ENGINE_4_CORRECTED_N2 = 684, 			// 
		TURB_ENGINE_4_CORRECTED_FF = 685, 			// 
		TURB_ENGINE_4_PCT_MAX_TORQUE = 686, 			// 
		TURB_ENGINE_4_EPR = 687, 			// 
		TURB_ENGINE_4_ITT = 688, 			// 
		TURB_ENGINE_4_AFTERBURNER = 689, 			// 
		TURB_ENGINE_4_JET_THRUST = 690, 			// 
		TURB_ENGINE_4_BLEEDAIRPSI = 691, 			// 
		TURB_ENGINE_4_TANK_SELECTOR = 692, 			// 
		TURB_ENGINE_4_TANKS_USED = 693, 			// 
		TURB_ENGINE_4_NUMBER_TANKS = 694, 			// 
		TURB_ENGINE_4_FUELFLOW_PPH = 695, 			// 
		TURB_ENGINE_4_FUEL_AVAILABLE = 696, 			// 
		TURB_ENGINE_4_PCT_AREA = 697, 			// 
		TURB_ENGINE_4_PCT_REVERSER = 698, 			// 
		TURB_ENGINE_4_VIBRATION = 699, 			// 
		PROPELLER_1_RPM = 700, 			// Propeller 1
		PROPELLER_1_PCT_MAX_RPM = 701, 			// 
		PROPELLER_1_THRUST = 702, 			// 
		PROPELLER_1_BETA = 703, 			// 
		PROPELLER_1_FEATHERING_INHIBIT = 704, 			// 
		PROPELLER_1_FEATHERED = 705, 			// 
		PROPELLER_1_SYNC_DELTA_LEVER = 706, 			// 
		PROPELLER_1_AUTOFEATHER_ARMED = 707, 			// 
		PROPELLER_2_RPM = 708, 			// Propeller 2
		PROPELLER_2_PCT_MAX_RPM = 709, 			// 
		PROPELLER_2_THRUST = 710, 			// 
		PROPELLER_2_BETA = 711, 			// 
		PROPELLER_2_FEATHERING_INHIBIT = 712, 			// 
		PROPELLER_2_FEATHERED = 713, 			// 
		PROPELLER_2_SYNC_DELTA_LEVER = 714, 			// 
		PROPELLER_2_AUTOFEATHER_ARMED = 715, 			// 
		PROPELLER_3_RPM = 716, 			// Propeller 3
		PROPELLER_3_PCT_MAX_RPM = 717, 			// 
		PROPELLER_3_THRUST = 718, 			// 
		PROPELLER_3_BETA = 719, 			// 
		PROPELLER_3_FEATHERING_INHIBIT = 720, 			// 
		PROPELLER_3_FEATHERED = 721, 			// 
		PROPELLER_3_SYNC_DELTA_LEVER = 722, 			// 
		PROPELLER_3_AUTOFEATHER_ARMED = 723, 			// 
		PROPELLER_4_RPM = 724, 			// Propeller 4
		PROPELLER_4_PCT_MAX_RPM = 725, 			// 
		PROPELLER_4_THRUST = 726, 			// 
		PROPELLER_4_BETA = 727, 			// 
		PROPELLER_4_FEATHERING_INHIBIT = 728, 			// 
		PROPELLER_4_FEATHERED = 729, 			// 
		PROPELLER_4_SYNC_DELTA_LEVER = 730, 			// 
		PROPELLER_4_AUTOFEATHER_ARMED = 731, 			// 
		HYDRAULICS1_PRESSURE_PSF = 732, 			// 
		HYDRAULICS1_RESERVOIR_PCT = 733, 			// 
		HYDRAULICS2_PRESSURE_PSF = 734, 			// 
		HYDRAULICS2_RESERVOIR_PCT = 735, 			// 
		HYDRAULICS3_PRESSURE_PSF = 736, 			// 
		HYDRAULICS3_RESERVOIR_PCT = 737, 			// 
		HYDRAULICS4_PRESSURE_PSF = 738, 			// 
		HYDRAULICS4_RESERVOIR_PCT = 739, 			// 
		AVIONICS_MASTER_SWITCH = 740, 			// 
		PANEL_AUTOFEATHER_SWITCH = 741, 			// Panel States
		PANEL_ANTIICE_SWITCH = 742, 			// 
		PANEL_AUTOBRAKE_SWITCH = 743, 			// 
		DAY_OF_WEEK = 744, 			// 0-6
		DAY_OF_MONTH = 745, 			// 1-31
		CURRENT_MONTH = 746, 			// 1-12
		CURRENT_YEAR = 747, 			// 1-????
		TIME_ZONE_OFFSET = 748, 			// in minutes, positive west of GMT
		DAYLIGHT_SAVING_ACTIVE = 749, 			// 0 or 1
		TOTAL_LOAD_AMPS = 750, 			// 
		BATTERY_LOAD = 751, 			// 
		BATTERY_VOLTAGE = 752, 			// 
		MAIN_BUS_VOLTAGE = 753, 			// 
		MAIN_BUS_AMPS = 754, 			// 
		AVIONICS_BUS_VOLTAGE = 755, 			// 
		AVIONICS_BUS_AMPS = 756, 			// 
		HOT_BATTERY_BUS_VOLTAGE = 757, 			// 
		HOT_BATTERY_BUS_AMPS = 758, 			// 
		BATTERY_BUS_VOLTAGE = 759, 			// 
		BATTERY_BUS_AMPS = 760, 			// 
		GENERATOR_ALTERNATOR_1_BUS_VOLTAGE = 761, 			// 
		GENERATOR_ALTERNATOR_1_BUS_AMPS = 762, 			// 
		GENERATOR_ALTERNATOR_2_BUS_VOLTAGE = 763, 			// 
		GENERATOR_ALTERNATOR_2_BUS_AMPS = 764, 			// 
		GENERATOR_ALTERNATOR_3_BUS_VOLTAGE = 765, 			// 
		GENERATOR_ALTERNATOR_3_BUS_AMPS = 766, 			// 
		GENERATOR_ALTERNATOR_4_BUS_VOLTAGE = 767, 			// 
		GENERATOR_ALTERNATOR_4_BUS_AMPS = 768, 			// 
		GENERAL_PANEL_CIRCUIT_ON = 769, 			// 
		FLAP_MOTOR_CIRCUIT_ON = 770, 			// 
		GEAR_MOTOR_CIRCUIT_ON = 771, 			// 
		AUTOPILOT_CIRCUIT_ON = 772, 			// 
		AVIONICS_CIRCUIT_ON = 773, 			// 
		PITOT_HEAT_CIRCUIT_ON = 774, 			// 
		PROP_SYNC_CIRCUIT_ON = 775, 			// 
		AUTO_FEATHER_CIRCUIT_ON = 776, 			// 
		AUTO_BRAKES_CIRCUIT_ON = 777, 			// 
		STANDBY_VACUUM_CIRCUIT_ON = 778, 			// 
		MARKER_BEACON_CIRCUIT_ON = 779, 			// 
		GEAR_WARNING_CIRCUIT_ON = 780, 			// 
		HYDRAULIC_PUMP_CIRCUIT_ON = 781, 			// 
		ELEVATOR_DEFLECTION = 782, 			// radians
		ELEVATOR_TRIM = 783, 			// radians
		AILERON_DEFLECTION = 784, 			// radians
		AILERON_TRIM = 785, 			// radians
		RUDDER_DEFLECTION = 786, 			// radians
		RUDDER_TRIM = 787, 			// radians
		PROPSYNC_ACTIVE = 788, 			// 
		INCIDENCE_ALPHA = 789, 			// 
		INCIDENCE_BETA = 790, 			// 
		TOTAL_FUEL_QUANTITY_LBS = 791, 			// 
		FLIGHT_DIRECTOR_ACTIVE = 792, 			// 
		FLIGHT_DIRECTOR_PITCH = 793, 			// degrees
		FLIGHT_DIRECTOR_BANK = 794, 			// degrees
		CG_PERCENT = 795, 			// 
		CG_AFT_LIMIT = 796, 			// 
		CG_FWD_LIMIT = 797, 			// 
		CG_MAX_MACH = 798, 			// 
		CG_MIN_MACH = 799, 			// 
		STANDARD_ATM_TEMPERATURE = 800, 			// standard atmosphere temperature at aircraft's altitude; celsius
		ENGINE_1_FUELFLOW_BUG_POSITION = 801, 			// lbs per hour
		ENGINE_2_FUELFLOW_BUG_POSITION = 802, 			// 
		ENGINE_3_FUELFLOW_BUG_POSITION = 803, 			// 
		ENGINE_4_FUELFLOW_BUG_POSITION = 804, 			// 
		CONCORDE_VISOR_NOSE_HANDLE = 805, 			// 0 - Visor Up, Nose Up, 1 - Visor Dn, Nose Up, 2 - Visor Dn, Nose 5 deg, 3 - Visor Dn, Nose 12.5 deg
		CONCORDE_VISOR_POS_PCT = 806, 			// 0=Up, 1.0 Extended/Down
		CONCORDE_NOSE_ANGLE = 807, 			// degrees; 0 - up
		GEAR_POS_TAIL = 808, 			// 
		ELEVON_1_DEFLECTION = 809, 			// The elevon indices start at 1 at the far left and increase to the right.
		ELEVON_2_DEFLECTION = 810, 			// radians
		ELEVON_3_DEFLECTION = 811, 			// 
		ELEVON_4_DEFLECTION = 812, 			// 
		ELEVON_5_DEFLECTION = 813, 			// 
		ELEVON_6_DEFLECTION = 814, 			// 
		ELEVON_7_DEFLECTION = 815, 			// 
		ELEVON_8_DEFLECTION = 816, 			// 
		PANEL_AUTOPILOT_SPEED_SETTING = 817, 			// Knots
		AUTOPILOT_AIRSPEED_ACQUISITION = 818, 			// boolean
		AUTOPILOT_AIRSPEED_HOLD_CURRENT = 819, 			// boolean
		AUTOPILOT_MAX_SPEED = 820, 			// boolean
		AUTOPILOT_CRUISE_SPEED = 821, 			// boolean
		BARBER_POLE_MACH = 822, 			// real mach
		SELECTED_FUEL_TRANSFER_MODE = 823, 			// -1 off, 0 auto, 1 forward, 2 aft
		HYDRAULIC_SYSTEM_INTEGRITY = 824, 			// 0.0 - complete failure; 1.0 - everything is ok
		ATTITUDE_CAGE_BUTTON = 825, 			// 
		VOR1_NEEDLE_RADIO = 826, 			// +-127 correspond to +-10 if VOR or to +-2.5 if localiser. Driven always by VOR
		GPS_POSITION_LAT = 827, 			// 
		GPS_POSITION_LON = 828, 			// 
		GPS_POSITION_ALT = 829, 			// 
		GPS_MAGVAR = 830, 			// magnetic deviation (radians)
		GPS_FLAGS = 831, 			// GPS_FLAG_ACTIVE_FLIGHT_PLAN = 0x00002, there is an active flight plan
		GPS_GROUND_SPEED = 832, 			// meters per second
		GPS_GROUND_HEADING = 833, 			// radians (0 - 2PI); true heading, yaw
		GPS_GROUND_TRACK = 834, 			// radians (0 - 2PI); heading + magvar, magnetic yaw
		GPS_WP_DISTANCE = 835, 			// meters to next wp
		GPS_WP_BEARING = 836, 			// heading (radians) to next wp + magvar
		GPS_WP_CROSS_TRK = 837, 			// cross_trk (meters)
		GPS_WP_REQ_HDG = 838, 			// required heading, magnetic, radians (0 - 2PI), from prev wp to next wp
		GPS_WP_STRING = 839, 			// name of next waypoint
		GPS_WP_NEXT_LAT = 840, 			// 
		GPS_WP_NEXT_LON = 841, 			// 
		GPS_WP_NEXT_ALT = 842, 			// 
		GPS_WP_PREV_LAT = 843, 			// 
		GPS_WP_PREV_LON = 844, 			// 
		GPS_WP_PREV_ALT = 845, 			// 
		GPS_WP_TRUE_BEARING = 846, 			// true bearing, degrees
		GPS_WP_TRUE_REQ_HDG = 847, 			// true required heading, degrees
		GPS_DRIVES_NAV1 = 848, 			// 
		HSI_VERTICAL_NEEDLE = 849, 			// -1.0 .. 1.0 corresponds to -10deg .. 10deg if VOR or to -2.5deg .. +2.5deg if localiser +5NM .. -5NM  if GPS
		HSI_HORIZONTAL_NEEDLE = 850, 			// -1.0 .. 1.0 corresponds to -0.7deg .. +0.7deg
		HSI_VERTICAL_VALID = 851, 			// 0 - Nav flag on
		HSI_HORIZONTAL_VALID = 852, 			// 0 - GS flag on
		HSI_TF_FLAG = 853, 			// -1.0 - from. 0.0 - none, 1.0 - to
		HSI_OBI_NEEDLE = 854, 			// 0 .. 360
		HSI_DESIRED_HEADING_NEEDLE = 855, 			// 0 .. 360
		HSI_BEARING = 856, 			// 0 .. 360
		HSI_BEARING_VALID = 857, 			// 1 - true
		HSI_SIGNAL_LOCALISER = 858, 			// 1 - true
		HSI_SPEED = 859, 			// knots
		HSI_DISTANCE = 860, 			// NM
		HSI_STATION_NAME = 861, 			// string
		GEAR_POS_AUX = 862, 			// Aux gear    0 - 16k
		TOOLTIP_UNITS = 863, 			// enum TOOLTIP_UNITS_TYPE
		LSO_TEXTURE_ID = 864, 			// current LSO_Texture_Id in AI_PLAYER.mw
		LSO_TEXT = 865, 			// text
		LEFT_FOLDING_WING_PERCENT = 866, 			// 0 - not folded; 1 - folded
		RIGHT_FOLDING_WING_PERCENT = 867, 			// 0 - not folded; 1 - folded
		MAIN_EXIT_OPEN = 868, 			// 0 - not open; 1 - open
		TAILHOOK_POSITION = 869, 			// 0 - retracted; 1 - extended
		COM2_FREQUENCY = 870, 			// BCD same as COM_FREQUENCY
		COM2_STBY_FREQUENCY = 871, 			// BCD; same as COM_FREQUENCY
		NAV1_STBY_FREQUENCY = 872, 			// BCD NAV1 radio freq; same as NAV1_FREQUENCY
		NAV2_STBY_FREQUENCY = 873, 			// BCD NAV2 radio freq; same as NAV2_FREQUENCY
		COM1_TRANSMIT_SELECT = 874, 			// 1.0 - yes
		COM2_TRANSMIT_SELECT = 875, 			// 1.0 - yes
		COM_BOTH_RECEIVE = 876, 			// 1.0 - yes
		NAV1_SOUND_TOGGLE = 877, 			// 
		NAV2_SOUND_TOGGLE = 878, 			// 
		DME_SOUND_TOGGLE = 879, 			// 
		ADF_SOUND_TOGGLE = 880, 			// 
		MARKER_SOUND_TOGGLE = 881, 			// 
		VOR2_GS_NEEDLE = 882, 			// 
		GENERAL_ENGINE1_ANTI_ICE_POS = 883, 			// 0 - off 1 - on
		GENERAL_ENGINE2_ANTI_ICE_POS = 884, 			// 0 - off 1 - on
		GENERAL_ENGINE3_ANTI_ICE_POS = 885, 			// 0 - off 1 - on
		GENERAL_ENGINE4_ANTI_ICE_POS = 886, 			// 0 - off 1 - on
		ENGINE1_ON_FIRE = 887, 			// 0 - no, 1 - yes
		ENGINE2_ON_FIRE = 888, 			// 
		ENGINE3_ON_FIRE = 889, 			// 
		ENGINE4_ON_FIRE = 890, 			// 
		ENGINE1_FUEL_PUMP_ON = 891, 			// 0 - no, 1 - yes
		ENGINE2_FUEL_PUMP_ON = 892, 			// 0 - no, 1 - yes
		ENGINE3_FUEL_PUMP_ON = 893, 			// 0 - no, 1 - yes
		ENGINE4_FUEL_PUMP_ON = 894, 			// 0 - no, 1 - yes
		GENERAL_ENGINE1_FUEL_VALVE = 895, 			// 1 = Open, 0 = Closed
		GENERAL_ENGINE2_FUEL_VALVE = 896, 			// 1 = Open, 0 = Closed
		GENERAL_ENGINE3_FUEL_VALVE = 897, 			// 1 = Open, 0 = Closed
		GENERAL_ENGINE4_FUEL_VALVE = 898, 			// 1 = Open, 0 = Closed
		GENERAL_ENGINE1_FUEL_PRESSURE = 899, 			// 
		GENERAL_ENGINE2_FUEL_PRESSURE = 900, 			// 
		GENERAL_ENGINE3_FUEL_PRESSURE = 901, 			// 
		GENERAL_ENGINE4_FUEL_PRESSURE = 902, 			// 
		WATER_RUDDER_ON = 903, 			// 0 - up, 1 - down
		PROP_DEICE_SWITCH = 904, 			// 0 = off, 1 = on
		STRUCTURAL_DEICE_SWITCH = 905, 			// 0 = off, 1 = on 
		FUEL_TANK_SELECTOR_2 = 906, 			// see FUEL_TANK_SELECTOR_ enum values
		TRAILING_EDGE_FLAPS0_LEFT_ANGLE = 907, 			// Angle of primary trailing edge flaps
		TRAILING_EDGE_FLAPS0_RIGHT_ANGLE = 908, 			// Angle of primary trailing edge flaps
		ALTERNATE_STATIC_AIR_OPEN = 909, 			// Alternate static air source
		AILERON_TRIM_PCT = 910, 			// percent -1 to 1
		RUDDER_TRIM_PCT = 911, 			// percent -1 to 1
		LIGHT_NAV_ON = 912, 			// 
		LIGHT_BEACON_ON = 913, 			// 
		LIGHT_LANDING_ON = 914, 			// 
		LIGHT_TAXI_ON = 915, 			// 
		LIGHT_STROBE_ON = 916, 			// 
		LIGHT_PANEL_ON = 917, 			// 
		LIGHT_RECOGNITION_ON = 918, 			// 
		LIGHT_WING_ON = 919, 			// 
		LIGHT_LOGO_ON = 920, 			// 
		LIGHT_CABIN_ON = 921, 			// 
		TOTAL_VELOCITY = 922, 			// 
		TOTAL_WORLD_VELOCITY = 923, 			// 
		VARIOMETER_RATE = 924, 			// 
		VARIOMETER_SWITCH = 925, 			// 
		RELATIVE_WIND_VELOCITY_BODY_X = 926, 			// 
		RELATIVE_WIND_VELOCITY_BODY_Y = 927, 			// 
		RELATIVE_WIND_VELOCITY_BODY_Z = 928, 			// 
		ACCELERATION_WORLD_X = 929, 			// 
		ACCELERATION_WORLD_Y = 930, 			// 
		ACCELERATION_WORLD_Z = 931, 			// 
		ACCELERATION_BODY_X = 932, 			// 
		ACCELERATION_BODY_Y = 933, 			// 
		ACCELERATION_BODY_Z = 934, 			// 
		PLANE_ALT_ABOVE_GROUND = 935, 			// 
		MAGNETIC_COMPASS = 936, 			// 
		TURN_INDICATOR_RATE = 937, 			// 
		TURN_INDICATOR_SWITCH = 938, 			// 
		BRAKE_DEPENDENT_HYDRAULIC_PRESSURE = 939, 			// 
		FLY_BY_WIRE_ELAC_SWITCH = 940, 			// 
		FLY_BY_WIRE_FAC_SWITCH = 941, 			// 
		FLY_BY_WIRE_SEC_SWITCH = 942, 			// 
		FLY_BY_WIRE_ELAC_FAILED = 943, 			// 
		FLY_BY_WIRE_FAC_FAILED = 944, 			// 
		FLY_BY_WIRE_SEC_FAILED = 945, 			// 
		GEAR_EMERGENCY_HANDLE_POSITION = 946, 			// 
		GEAR_WARNING = 947, 			// 
		TAILWHEEL_LOCK_ON = 948, 			// 
		ANTISKID_BRAKES_ACTIVE = 949, 			// 
		RETRACT_FLOAT_SWITCH = 950, 			// 
		STEER_INPUT_CONTROL = 951, 			// 
		STALL_ALPHA = 952, 			// 
		DECISION_ALTITUDE_MSL = 954, 			// 
		WATER_BALLAST_VALVE = 955, 			// 
		AUTOPILOT_RPM_HOLD = 956, 			// 
		AUTOPILOT_PITCH_HOLD_REF = 957, 			// 
		AUTOPILOT_MAX_BANK = 958, 			// 
		PROP_FEATHER_SWITCH = 959, 			// 
		MASTER_IGNITION_SWITCH = 960, 			// 
		BLEED_AIR_SOURCE_CONTROL = 961, 			// 
		STRUCTURAL_ICE_PCT = 962, 			// 
		ROTOR_BRAKE_HANDLE_POS = 963, 			// 
		ROTOR_BRAKE_ACTIVE = 964, 			// 
		ROTOR_CLUTCH_SWITCH_POS = 965, 			// 
		ROTOR_CLUTCH_ACTIVE = 966, 			// 
		ROTOR_GOV_SWITCH_POS = 967, 			// 
		ROTOR_GOV_ACTIVE = 968, 			// 
		ROTOR_LATERAL_TRIM_PCT = 969, 			// 
		DISK_PITCH_ANGLE = 970, 			// 
		DISK_BANK_ANGLE = 971, 			// 
		DISK_PITCH_PCT = 972, 			// 
		DISK_BANK_PCT = 973, 			// 
		TOW_RELEASE_HANDLE = 974, 			// 
		APU_PCT_RPM = 975, 			// 
		APU_PCT_STARTER = 976, 			// 
		APU_VOLTS = 977, 			// 
		APU_GENERATOR_SWITCH = 978, 			// 
		APU_GENERATOR_ACTIVE = 979, 			// 
		APU_ON_FIRE_DETECTED = 980, 			// 
		PRESSURIZATION_CABIN_ALTITUDE = 981, 			// 
		PRESSURIZATION_CABIN_ALTITUDE_GOAL = 982, 			// 
		PRESSURIZATION_CABIN_ALTITUDE_RATE = 983, 			// 
		PRESSURIZATION_PRESSURE_DIFFERENTIAL = 984, 			// 
		PRESSURIZATION_DUMP_SWITCH = 985, 			// 
		FIRE_BOTTLE_SWITCH = 986, 			// 
		FIRE_BOTTLE_DISCHARGED = 987, 			// 
		CABIN_NO_SMOKING_ALERT_SWITCH = 988, 			// 
		CABIN_SEATBELTS_ALERT_SWITCH = 989, 			// 
		GPWS_WARNING = 990, 			// 
		GPWS_SYSTEM_ACTIVE = 991, 			// 
		IS_LATITUDE_LONGITUDE_FREEZE_ON = 992, 			// 
		IS_ALTITUDE_FREEZE_ON = 993, 			// 
		NOSEWHEEL_LOCK_ON = 994, 			// 
		PAYLOAD_STATION_NUM_SIMOBJECTS = 995, 			// 
		FOLDING_WING_HANDLE_POSITION = 996, 			// 
		TAILHOOK_HANDLE = 997, 			// 
		LAUNCHBAR_POSITION = 998, 			// 
		LAUNCHBAR_SWITCH = 999, 			// 
		LAUNCHBAR_HELD_EXTENDED = 1000, 			// 
		AUTOPILOT_PITCH_HOLD = 1001, 			// 
		NUMBER_OF_CATAPULTS = 1002, 			// 
		HOLDBACK_BAR_INSTALLED = 1003, 			// 
		BLAST_SHIELD_POSITION = 1004, 			// 
		CATAPULT_STROKE_POSITION = 1005, 			// 
		RECIP_ENG_DETONATING = 1006, 			// 
		RECIP_ENG_CYLINDER_HEALTH = 1007, 			// 
		RECIP_ENG_NUM_CYLINDERS = 1008, 			// 
		RECIP_ENG_NUM_CYLINDERS_FAILED = 1009, 			// 
		RECIP_ENG_ANTIDETONATION_TANK_VALVE = 1010, 			// 
		RECIP_ENG_ANTIDETONATION_TANK_QUANTITY = 1011, 			// 
		RECIP_ENG_ANTIDETONATION_TANK_MAX_QUANTITY = 1012, 			// 
		RECIP_ENG_NITROUS_TANK_VALVE = 1013, 			// 
		RECIP_ENG_NITROUS_TANK_QUANTITY = 1014, 			// 
		RECIP_ENG_NITROUS_TANK_MAX_QUANTITY = 1015, 			// 
		TURB_ENG_AFTERBURNER_STAGE_ACTIVE = 1016, 			// 
		TURB_ENG_AFTERBURNER_PCT_ACTIVE = 1017, 			// 
		FUEL_DUMP_SWITCH = 1018, 			// 
		FUEL_DUMP_ACTIVE = 1019, 			// 
		COLLECTIVE_POSITION = 1020, 			// 
		IS_ATTITUDE_FREEZE_ON = 1021, 			// 
		NUM_SLING_CABLES = 1022, 			// 
		SLING_OBJECT_ATTACHED = 1023, 			// 
		SLING_CABLE_BROKEN = 1024, 			// 
		SLING_CABLE_EXTENDED_LENGTH = 1025, 			// 
		SLING_ACTIVE_PAYLOAD_STATION = 1026, 			// 
		SLING_HOIST_PERCENT_DEPLOYED = 1027, 			// 
		SLING_HOIST_SWITCH = 1028, 			// 
		SLING_HOOK_IN_PICKUP_MODE = 1029, 			// 
		CABLE_CAUGHT_BY_TAILHOOK = 1030, 			// 
		ADF_VOLUME = 1031, 			// 
		AILERON_TRIM_DISABLED = 1032, 			// 
		ALTERNATOR_BREAKER_PULLED = 1033, 			// 
		ALTERNATOR_CONNECTION_ON = 1034, 			// 
		ANIMATION_DELTA_TIME = 1035, 			// 
		ANNUNCIATOR_SWITCH = 1036, 			// 
		APU_BLEED_PRESSURE_RECEIVED_BY_ENGINE = 1037, 			// 
		APU_SWITCH = 1038, 			// 
		ATC_AIRPORT_IS_TOWERED = 1039, 			// 
		ATC_CLEARED_LANDING = 1040, 			// 
		ATC_CLEARED_TAKEOFF = 1041, 			// 
		ATC_CLEARED_TAXI = 1042, 			// 
		ATC_RUNWAY_AIRPORT_NAME = 1043, 			// 
		ATC_RUNWAY_DISTANCE = 1044, 			// 
		ATC_RUNWAY_END_DISTANCE = 1045, 			// 
		ATC_RUNWAY_HEADING_DEGREES_TRUE = 1046, 			// 
		ATC_RUNWAY_LENGTH = 1047, 			// 
		ATC_RUNWAY_RELATIVE_POSITION_X = 1048, 			// 
		ATC_RUNWAY_RELATIVE_POSITION_Y = 1049, 			// 
		ATC_RUNWAY_RELATIVE_POSITION_Z = 1050, 			// 
		ATC_RUNWAY_SELECTED = 1051, 			// 
		ATC_RUNWAY_START_DISTANCE = 1052, 			// 
		ATC_RUNWAY_TDPOINT_RELATIVE_POSITION_X = 1053, 			// 
		ATC_RUNWAY_TDPOINT_RELATIVE_POSITION_Y = 1054, 			// 
		ATC_RUNWAY_TDPOINT_RELATIVE_POSITION_Z = 1055, 			// 
		ATC_RUNWAY_WIDTH = 1056, 			// 
		AUDIO_PANEL_VOLUME = 1057, 			// 
		AUTOBRAKES_ACTIVE = 1058, 			// 
		AUTOPILOT_AIRSPEED_MAX_CALCULATED = 1059, 			// Can be set to limit the maximum airspeed of the AP
		AUTOPILOT_AIRSPEED_MIN_CALCULATED = 1060, 			// Can be set to limit the minimum airspeed of the AP
		AUTOPILOT_ALTITUDE_ARM = 1061, 			// 
		AUTOPILOT_ALTITUDE_SLOT_INDEX = 1062, 			// 
		AUTOPILOT_APPROACH_ACTIVE = 1063, 			// 
		AUTOPILOT_APPROACH_ARM = 1064, 			// 
		AUTOPILOT_APPROACH_CAPTURED = 1065, 			// 
		AUTOPILOT_BANK_MODE = 1066, 			// bank mode on/off
		AUTOPILOT_DISENGAGED = 1067, 			// 
		AUTOPILOT_GLIDESLOPE_ACTIVE = 1068, 			// 
		AUTOPILOT_GLIDESLOPE_ARM = 1069, 			// 
		AUTOPILOT_HEADING_SLOT_INDEX = 1070, 			// 
		AUTOPILOT_MANAGED_INDEX = 1071, 			// 
		AUTOPILOT_MANAGED_SPEED_IN_MACH = 1072, 			// 
		AUTOPILOT_MANAGED_THROTTLE_ACTIVE = 1073, 			// 
		AUTOPILOT_MAX_BANK_ID = 1074, 			// 
		AUTOPILOT_RPM_SLOT_INDEX = 1075, 			// 
		AUTOPILOT_SPEED_SLOT_INDEX = 1076, 			// 
		AUTOPILOT_THROTTLE_MAX_THRUST = 1077, 			// 
		AUTOPILOT_VS_SLOT_INDEX = 1078, 			// 
		BAGGAGELOADER_ANGLE_CURRENT = 1079, 			// 
		BAGGAGELOADER_ANGLE_TARGET = 1080, 			// 
		BAGGAGELOADER_END_RAMP_Y = 1081, 			// 
		BAGGAGELOADER_END_RAMP_Z = 1082, 			// 
		BAGGAGELOADER_PIVOT_Y = 1083, 			// 
		BAGGAGELOADER_PIVOT_Z = 1084, 			// 
		BATTERY_BREAKER_PULLED = 1085, 			// 
		BATTERY_CONNECTION_ON = 1086, 			// 
		BLEED_AIR_APU = 1087, 			// 
		BLEED_AIR_ENGINE = 1088, 			// 
		BOARDINGRAMP_ELEVATION_CURRENT = 1089, 			// 
		BOARDINGRAMP_ELEVATION_TARGET = 1090, 			// 
		BOARDINGRAMP_END_POSITION_Y = 1091, 			// 
		BOARDINGRAMP_END_POSITION_Z = 1092, 			// 
		BOARDINGRAMP_ORIENTATION_CURRENT = 1093, 			// 
		BOARDINGRAMP_ORIENTATION_TARGET = 1094, 			// 
		BOARDINGRAMP_START_POSITION_Y = 1095, 			// 
		BOARDINGRAMP_START_POSITION_Z = 1096, 			// 
		BRAKES_LEFT_POS_EX1 = 1097, 			// 0=off 32k=full brakes
		BRAKES_RIGHT_POS_EX1 = 1098, 			// 0=off 32k=full brakes
		BREAKER_ADF = 1099, 			// 
		BREAKER_ALTFLD = 1100, 			// 
		BREAKER_AUTOPILOT = 1101, 			// 
		BREAKER_AVNBUS1 = 1102, 			// 
		BREAKER_AVNBUS2 = 1103, 			// 
		BREAKER_AVNFAN = 1104, 			// 
		BREAKER_FLAP = 1105, 			// 
		BREAKER_GPS = 1106, 			// 
		BREAKER_INST = 1107, 			// 
		BREAKER_INSTLTS = 1108, 			// 
		BREAKER_LTS_PWR = 1109, 			// 
		BREAKER_NAVCOM1 = 1110, 			// 
		BREAKER_NAVCOM2 = 1111, 			// 
		BREAKER_NAVCOM3 = 1112, 			// 
		BREAKER_TURNCOORD = 1113, 			// 
		BREAKER_WARN = 1114, 			// 
		BREAKER_XPNDR = 1115, 			// 
		BUS_BREAKER_PULLED = 1116, 			// 
		BUS_CONNECTION_ON = 1117, 			// 
		BUS_LOOKUP_INDEX = 1118, 			// 
		CATERINGTRUCK_AIRCRAFT_DOOR_CONTACT_OFFSET_Z = 1119, 			// 
		CATERINGTRUCK_ELEVATION_CURRENT = 1120, 			// 
		CATERINGTRUCK_ELEVATION_TARGET = 1121, 			// 
		CATERINGTRUCK_OPENING_CURRENT = 1122, 			// 
		CATERINGTRUCK_OPENING_TARGET = 1123, 			// 
		CIRCUIT_BREAKER_PULLED = 1124, 			// 
		CIRCUIT_CONNECTION_ON = 1125, 			// 
		CIRCUIT_NAVCOM1_ON = 1126, 			// 
		CIRCUIT_NAVCOM2_ON = 1127, 			// 
		CIRCUIT_NAVCOM3_ON = 1128, 			// 
		CIRCUIT_ON = 1129, 			// 
		CIRCUIT_SWITCH_ON = 1130, 			// 
		COM1_STORED_FREQUENCY = 1131, 			// 
		COM2_STORED_FREQUENCY = 1132, 			// 
		COM3_STORED_FREQUENCY = 1133, 			// 
		COM_ACTIVE_FREQUENCY = 1134, 			// 
		COM_RECEIVE = 1135, 			// 
		COM_STANDBY_FREQUENCY = 1136, 			// 
		COM_VOLUME = 1137, 			// 
		COPILOT_TRANSMITTER_TYPE = 1138, 			// 
		COPILOT_TRANSMITTING = 1139, 			// 
		ELEVATOR_TRIM_DISABLED = 1140, 			// 
		ELEVATOR_TRIM_NEUTRAL = 1141, 			// 
		ELT_ACTIVE = 1142, 			// 
		EXTERNAL_POWER_AVAILABLE = 1143, 			// 
		EXTERNAL_POWER_BREAKER_PULLED = 1144, 			// 
		EXTERNAL_POWER_CONNECTION_ON = 1145, 			// 
		EXTERNAL_POWER_ON = 1146, 			// 
		FLIGHT_DIRECTOR_BANK_EX1 = 1147, 			// degrees
		FLIGHT_DIRECTOR_PITCH_EX1 = 1148, 			// degrees
		FUELSYSTEM_ENGINE_PRESSURE = 1149, 			// 
		FUELSYSTEM_JUNCTION_SETTING = 1150, 			// 
		FUELSYSTEM_LINE_FUEL_FLOW = 1151, 			// 
		FUELSYSTEM_LINE_FUEL_LEVEL = 1152, 			// 
		FUELSYSTEM_LINE_FUEL_PRESSURE = 1153, 			// 
		FUELSYSTEM_PUMP_ACTIVE = 1154, 			// 
		FUELSYSTEM_PUMP_SWITCH = 1155, 			// 
		FUELSYSTEM_TANK_CAPACITY = 1156, 			// 
		FUELSYSTEM_TANK_LEVEL = 1157, 			// 
		FUELSYSTEM_TANK_QUANTITY = 1158, 			// 
		FUELSYSTEM_TANK_WEIGHT = 1159, 			// 
		FUELSYSTEM_TRIGGER_STATUS = 1160, 			// 
		FUELSYSTEM_VALVE_OPEN = 1161, 			// 
		FUELSYSTEM_VALVE_SWITCH = 1162, 			// 
		FUELTRUCK_HOSE_DEPLOYED = 1163, 			// 
		FUELTRUCK_HOSE_END_POSX = 1164, 			// 
		FUELTRUCK_HOSE_END_POSZ = 1165, 			// 
		FUELTRUCK_HOSE_END_RELATIVE_HEADING = 1166, 			// 
		FUEL_TRANSFERT_PUMP_ON = 1167, 			// 
		GENERAL_ENGINE1_THROTTLE_MANAGED_MODE = 1168, 			// 
		GENERAL_ENGINE2_THROTTLE_MANAGED_MODE = 1169, 			// 
		GENERAL_ENGINE3_THROTTLE_MANAGED_MODE = 1170, 			// 
		GENERAL_ENGINE4_THROTTLE_MANAGED_MODE = 1171, 			// 
		GENERAL_ENG_COMBUSTION_EX1 = 1172, 			// 
		GENERAL_ENG_FUEL_PUMP_SWITCH_EX1 = 1173, 			// 
		GENERAL_ENG_HOBBS_ELAPSED_TIME = 1174, 			// 
		GPS_FLIGHTPLAN_TOTAL_DISTANCE = 1175, 			// 
		GROUNDPOWERUNIT_HOSE_DEPLOYED = 1176, 			// 
		GROUNDPOWERUNIT_HOSE_END_POSX = 1177, 			// 
		GROUNDPOWERUNIT_HOSE_END_POSZ = 1178, 			// 
		GROUNDPOWERUNIT_HOSE_END_RELATIVE_HEADING = 1179, 			// 
		INTERACTIVE_POINT_BANK = 1180, 			// 
		INTERACTIVE_POINT_HEADING = 1181, 			// 
		INTERACTIVE_POINT_JETWAY_LEFT_BEND = 1182, 			// 
		INTERACTIVE_POINT_JETWAY_LEFT_DEPLOYMENT = 1183, 			// 
		INTERACTIVE_POINT_JETWAY_RIGHT_BEND = 1184, 			// 
		INTERACTIVE_POINT_JETWAY_RIGHT_DEPLOYMENT = 1185, 			// 
		INTERACTIVE_POINT_JETWAY_TOP_HORIZONTAL = 1186, 			// 
		INTERACTIVE_POINT_JETWAY_TOP_VERTICAL = 1187, 			// 
		INTERACTIVE_POINT_OPEN = 1188, 			// 
		INTERACTIVE_POINT_PITCH = 1189, 			// 
		INTERACTIVE_POINT_POSX = 1190, 			// 
		INTERACTIVE_POINT_POSY = 1191, 			// 
		INTERACTIVE_POINT_POSZ = 1192, 			// 
		INTERACTIVE_POINT_TYPE = 1193, 			// 
		INTERCOM_MODE = 1194, 			// 
		INTERCOM_SYSTEM_ACTIVE = 1195, 			// 
		IS_ANY_INTERIOR_LIGHT_ON = 1196, 			// 
		JETWAY_HOOD_LEFT_BEND = 1197, 			// 
		JETWAY_HOOD_LEFT_DEPLOYMENT = 1198, 			// 
		JETWAY_HOOD_RIGHT_BEND = 1199, 			// 
		JETWAY_HOOD_RIGHT_DEPLOYMENT = 1200, 			// 
		JETWAY_HOOD_TOP_HORIZONTAL = 1201, 			// 
		JETWAY_HOOD_TOP_VERTICAL = 1202, 			// 
		JETWAY_MOVING = 1203, 			// 
		JETWAY_WHEEL_ORIENTATION_CURRENT = 1204, 			// 
		JETWAY_WHEEL_ORIENTATION_TARGET = 1205, 			// 
		JETWAY_WHEEL_SPEED = 1206, 			// 
		KOHLSMAN_SETTING_STD = 1207, 			// 
		LIGHT_BACKLIGHT_INTENSITY = 1208, 			// 
		LIGHT_GLARESHIELD = 1209, 			// 
		LIGHT_GYROLIGHT_INTENSITY = 1210, 			// 
		LIGHT_HEADLIGHT_INTENSITY = 1211, 			// 
		LIGHT_PEDESTRAL = 1212, 			// 
		LIGHT_POTENTIOMETER = 1213, 			// 
		MARKER_BEACON_SENSITIVITY_HIGH = 1214, 			// 
		MARKER_BEACON_TEST_MUTE = 1215, 			// 
		MISSION_SCORE = 1216, 			// 
		NAV_VOLUME = 1217, 			// 
		ON_ANY_RUNWAY = 1218, 			// 
		PILOT_TRANSMITTER_TYPE = 1219, 			// 
		PILOT_TRANSMITTING = 1220, 			// 
		PITOT_HEAT_SWITCH = 1221, 			// 0=Off, 1=On, 2=Auto
		PLANE_ALT_ABOVE_GROUND_MINUS_CG = 1222, 			// 
		PLANE_IN_PARKING_STATE = 1223, 			// 
		PLANE_TOUCHDOWN_BANK_DEGREES = 1224, 			// 
		PLANE_TOUCHDOWN_HEADING_DEGREES_MAGNETIC = 1225, 			// 
		PLANE_TOUCHDOWN_HEADING_DEGREES_TRUE = 1226, 			// 
		PLANE_TOUCHDOWN_LATITUDE = 1227, 			// 
		PLANE_TOUCHDOWN_LONGITUDE = 1228, 			// 
		PLANE_TOUCHDOWN_NORMAL_VELOCITY = 1229, 			// 
		PLANE_TOUCHDOWN_PITCH_DEGREES = 1230, 			// 
		PUSHBACK_ATTACHED = 1231, 			// 
		PUSHBACK_AVAILABLE = 1232, 			// 
		RECIP_ENG_ENGINE_MASTER_SWITCH = 1233, 			// 
		RECIP_ENG_GLOW_PLUG_ACTIVE = 1234, 			// 
		REJECTED_TAKEOFF_BRAKES_ACTIVE = 1235, 			// 
		RUDDER_TRIM_DISABLED = 1236, 			// 
		SHUTOFF_VALVE_PULLED = 1237, 			// 
		SLOPE_TO_ATC_RUNWAY = 1238, 			// 
		SPEAKER_ACTIVE = 1239, 			// 
		TOTAL_UNUSABLE_FUEL_QUANTITY_GALLONS = 1240, 			// 
		TURB_ENGINE_1_COMMANDED_N1 = 1241, 			// 
		TURB_ENGINE_1_FREE_TURBINE_TORQUE = 1242, 			// 
		TURB_ENGINE_1_THROTTLE_COMMANDED_N1 = 1243, 			// 
		TURB_ENGINE_2_COMMANDED_N1 = 1244, 			// 
		TURB_ENGINE_2_FREE_TURBINE_TORQUE = 1245, 			// 
		TURB_ENGINE_2_THROTTLE_COMMANDED_N1 = 1246, 			// 
		TURB_ENGINE_3_COMMANDED_N1 = 1247, 			// 
		TURB_ENGINE_3_FREE_TURBINE_TORQUE = 1248, 			// 
		TURB_ENGINE_3_THROTTLE_COMMANDED_N1 = 1249, 			// 
		TURB_ENGINE_4_COMMANDED_N1 = 1250, 			// 
		TURB_ENGINE_4_FREE_TURBINE_TORQUE = 1251, 			// 
		TURB_ENGINE_4_THROTTLE_COMMANDED_N1 = 1252, 			// 
		TURB_ENG_IGNITION_SWITCH_EX1 = 1253, 			// 
		TURB_ENG_IS_IGNITING = 1254, 			// 
		TURN_COORDINATOR_BALL_POS_INV = 1255, 			// turn coordinator ball position {-127..0..127} (inverted)
		WAGON_BACK_LINK_LENGTH = 1256, 			// 
		WAGON_BACK_LINK_ORIENTATION = 1257, 			// 
		WAGON_BACK_LINK_START_POSZ = 1258, 			// 
		WAGON_FRONT_LINK_LENGTH = 1259, 			// 
		WAGON_FRONT_LINK_ORIENTATION = 1260, 			// 
		WAGON_FRONT_LINK_START_POSZ = 1261, 			// 
		WINDSHIELD_DEICE_SWITCH = 1262, 			// 0 = off, 1 = on 
		WINDSHIELD_WIND_VELOCITY = 1263, 			// 
		GPS_OBS_ACTIVE = 1264, 			// 
		GPS_OBS_VALUE = 1265, 			// 
		HAND_ANIM_STATE = 1266, 			// 
		FUELSYSTEM_TANK_TOTAL_QUANTITY = 1267, 			// 
		BATTERY_ESTIMATED_CAPACITY_PCT = 1268, 			// 
		CONTACT_POINT_COMPRESSION = 1269, 			// Contact point    0 - 16k
		AUTOPILOT_AVIONICS_MANAGED = 1270, 			// Avionics managed autopilot state mode active
		AUTOPILOT_BANK_HOLD_REF = 1271, 			// 
		CONTACT_POINT_POSITION = 1272, 			// Contact point    0 - 16k
		OIL_COOLING_FLAPS_POSITION = 1273, 			// 
		RADIATOR_COOLING_FLAPS_POSITION = 1274, 			// 
		INTERACTIVE_POINT_GOAL = 1275, 			// 
		COM_RECEIVE_EX1 = 1276, 			// 
		RECIP_ENG_ANTIDETONATION_FLOW_RATE = 1277, 			// 
		CIRCUIT_COM_ON = 1278, 			// 
		CIRCUIT_NAV_ON = 1279, 			// 
		YOKE_POS_X_LINEAR = 1280, 			// (OBSOLETE)
		TACAN_DRIVES_NAV1 = 1281, 			// 
		RACE_LAST_LAP_TIME = 1282, 			// 
		CIRCUIT_NAVCOM4_ON = 1283, 			// 
		BREAKER_NAVCOM4 = 1284, 			// 
		AUTOPILOT_APPROACH_IS_LOCALIZER = 1285, 			// 
        C_GAUGE_TOKEN                               // Always the last
            
    } GAUGE_TOKEN;

typedef enum    VAR_TYPE {
	VAR_TYPE_NONE,

	TYPE_BOOL8,
	TYPE_UINT8,
	TYPE_SINT8,
	TYPE_FLAGS8,
	TYPE_ENUM8,

	TYPE_BOOL16,
	TYPE_ANGL16,
	TYPE_UINT16,
	TYPE_SINT16,
	TYPE_UIF16,
	TYPE_SIF16,
	TYPE_FLAGS16,
	TYPE_ENUM16,
	TYPE_BCD16,
	TYPE_BCO16,
	TYPE_VAR16,

	TYPE_BOOL32,
	TYPE_ANGL32,
	TYPE_UINT32,
	TYPE_SINT32,
	TYPE_UIF32,
	TYPE_SIF32,
	TYPE_FLAGS32,
	TYPE_ENUM32,
	TYPE_VAR32,

	TYPE_ANGL48,
	TYPE_SINT48,
	TYPE_UIF48,
	TYPE_SIF48,

	TYPE_UINT64,
	TYPE_SINT64,
	TYPE_SIF64,
	TYPE_FLOAT64,

	TYPE_BOOL,
	TYPE_FLAGS,
	TYPE_ENUM,

	TYPE_VOID,
	TYPE_PVOID,

	TYPE_PUINT32,
	TYPE_PSINT32,
	TYPE_PFLOAT64,

	VAR_TYPE_MAX
} VAR_TYPE;

typedef union   UNIVERSAL_VAR {
	FLOAT64 n;              // any number
	BOOL    b;              // any boolean
	ENUM    e;              // any enumerated value
	FLAGS   f;              // any flags field
	PVOID   p;              // any pointer
	VAR32   d;              // any binary coded decimal
	VAR32   o;              // any binary coded octal
} UNIVERSAL_VAR, *PUNIVERSAL_VAR, **PPUNIVERSAL_VAR;

typedef struct  MODULE_VAR {
	GAUGE_TOKEN             id;
	PVOID                   var_ptr;
	VAR_TYPE                var_type;
	UNIVERSAL_VAR           var_value;
	UNIVERSAL_VAR           var_old;                // value of global var last iteration
} MODULE_VAR, *PMODULE_VAR, **PPMODULE_VAR;



// Panel identifiers
#define IDENT_MAIN_PANEL                        0
#define IDENT_MAIN_PANEL_ALT1                    1
#define IDENT_MAIN_PANEL_ALT2                    2
#define IDENT_MAIN_PANEL_ALT3                    3
#define IDENT_MAIN_PANEL_ALT4                    4
#define IDENT_MAIN_PANEL_ALT5                    5
#define IDENT_MAIN_PANEL_ALT6                    6
#define IDENT_MAIN_PANEL_ALT7                    7
#define IDENT_MAIN_PANEL_ALT8                    8
#define IDENT_MAIN_PANEL_ALT9                    9
#define IDENT_THROTTLE_PANEL                    10
#define IDENT_RADIO_STACK_PANEL                 50
#define IDENT_COMPASS_PANEL                     75
#define IDENT_MINI_CONTROLS_PANEL               100
#define IDENT_ANNUNCIATOR_PANEL                 125
#define IDENT_ANNUNCIATOR2_PANEL                150
#define IDENT_IFR_MAIN_PANEL                    175
#define IDENT_COLLECTIVE_PANEL                  200
#define IDENT_GPS_PANEL                         225
#define IDENT_OVERHEAD_PANEL                    250
#define IDENT_PARKING_BRAKE_PANEL               251
#define IDENT_FLAPS_TRIM_PANEL                  252
#define IDENT_FUEL_PANEL                        253
#define IDENT_ELECTRICAL_PANEL                  254
#define IDENT_TRIM_PANEL                        255
#define IDENT_LANDING_GEAR_PANEL                256
#define IDENT_MISC_POPUP_1                      260
#define IDENT_MISC_POPUP_2                      261
#define IDENT_MISC_POPUP_3                      262
#define IDENT_MISC_POPUP_4                      263
#define IDENT_MISC_POPUP_5                      264
#define IDENT_MISC_POPUP_6                      265
#define IDENT_MISC_POPUP_7                      266
#define IDENT_MISC_POPUP_8                      267
#define IDENT_MISC_POPUP_9                      268
#define IDENT_MISC_POPUP_10                     269


#define IDENT_USER                              10000
#define IDENT_USER_MAX                          19999

#define MIN_HUD_IDENT                           20000
#define IDENT_AIRCRAFT_HUD_3D_PANEL             20000
#define IDENT_HELICOPTER_HUD_3D_PANEL           20001
#define IDENT_CONTROLS_HUD_3D_PANEL             20002
#define IDENT_TEXT_HUD_3D_PANEL                 20003
#define IDENT_DAMAGE_HUD_3D_PANEL               20004
#define IDENT_SCORE_HUD_3D_PANEL                20005
#define MAX_HUD_IDENT                           20006

#define IDENT_LSO_WINDOW                        22000
#define IDENT_POI_WINDOW                        22001
#define IDENT_TIMER_WINDOW                      22002
#define IDENT_DROPPABLE_OBJ_WINDOW              22003


#define IDENT_INVALID                           4294967295

// HUD settings
#define HUD_ON                          0x1
#define HUD_METRIC                      0x2


// PANEL STATES
//
#define PS_MAIN_PANEL_VISIBLE           BIT1
#define PS_MAIN_PANEL_COMPOSED          BIT2
#define PS_PADLOCK_SERVER               BIT3            // not used yet
#define PS_PADLOCK_CLIENT               BIT4            // not used yet
#define PS_HELICOPTER_HUD_VISIBLE       BIT5
#define PS_AIRCRAFT_HUD_VISIBLE         BIT6
#define PS_AIRCRAFT_HUD_ON              BIT7
#define PS_RADIO_STACK_VISIBLE          BIT8
#define PS_HUD_METRIC_OUTPUT            BIT9
#define PS_MAIN_PANEL_ON                BIT10
#define PS_CONTROLS_HUD_VISIBLE         BIT11
#define PS_RADIO_STACK_AUTOPOPED        BIT12
#define PS_MAIN_PANEL_UNDOCKED          BIT13
#define PS_AIRCRAFT_HUD_EXIST           BIT14
#define PS_HELICOPTER_HUD_EXIST         BIT15
#define PS_HUD_EXIST                    (PS_AIRCRAFT_HUD_EXIST|PS_HELICOPTER_HUD_EXIST)
#define PS_PANELS_LOAD_VISIBLE          BIT16
#define PS_HUD_LOAD_VISIBLE             BIT17
#define PS_PANEL_VISIBLE                BIT18
#define PS_HUD_PANEL_VISIBLE            BIT19
#define PS_READONLY                      (~(PS_PADLOCK_SERVER|PS_PADLOCK_CLIENT|PS_HUD_METRIC_OUTPUT))

#define PS_SET                                          0xFFFFFFFF
#define PS_CLEAR                                        0x00000000

#define GAUGE_FLAG_NORMAL               0
#define GAUGE_FLAG_HIDDEN               0x1
#define GAUGE_FLAG_BLINKING             0x2
#define GAUGE_FLAG_GRAYED               0x4
#define GAUGE_FLAG_HILIGHTED            0x8

#define UNITS_UNKNOWN                   (-1)
#define UNITS_STRING                    (-2)

class IGaugeCDrawableCreateParameters
{
public:
	enum PARAMETER_TYPE {
		PARAMETER_TYPE_NONE = 0,
		PARAMETER_TYPE_BOOL = 1,
		PARAMETER_TYPE_FLOAT = 2,
		PARAMETER_TYPE_INT = 3,
		PARAMETER_TYPE_STRING = 4,
	};

	virtual bool GetParameter(const char* szParName, const char** pszValue) const = 0;
	virtual bool GetParameter(const char* szParName, FLOAT64* pdValue) const = 0;
	virtual bool GetParameter(const char* szParName, SINT32* piValue) const = 0;
	virtual bool GetParameter(const char* szParName, bool* pbValue) const = 0;
	virtual bool SetParameterId(const char* szParName, SINT32 id, PARAMETER_TYPE type) const = 0;
};

class IGaugeCDrawableDrawParameters
{
public:
	virtual bool GetParameter(SINT32 id, const char** pszValue) const = 0;
	virtual bool GetParameter(SINT32 id, FLOAT64* pdValue) const = 0;
	virtual bool GetParameter(SINT32 id, SINT32* piValue) const = 0;
	virtual bool GetParameter(SINT32 id, bool* pbValue) const = 0;
	virtual FLOAT64 GetScaleX() const = 0;
	virtual FLOAT64 GetScaleY() const = 0;
	virtual const PIXRECT* GetScreenRectangle() const = 0;         // return pointer to a rect relative to the flight sim window left top corner or NULL if undocked or virtual cockpit
};

class IGaugeCDrawable
{
public:
	virtual ULONG AddRef() = 0;
	virtual ULONG Release() = 0;
	virtual FLAGS32 GetFlags() = 0;
	virtual void Update() = 0;
	virtual void Show(bool on) = 0;
	virtual bool Draw(IGaugeCDrawableDrawParameters* pParameters, PIXPOINT size, HDC hdc, PIMAGE pImage) = 0;
	virtual bool SetupDraw(PIXPOINT size, HDC hdc, PIMAGE pImage) = 0;
	virtual bool GetDraw(IGaugeCDrawableDrawParameters* pParameters) = 0;

	// flags returened by GetFlags
	enum {
		TAKES_DC = 0x1,             // Draw() method should be called with a valid hdc
		TAKES_PIMAGE = 0x2,         // Draw() method should be called with a pImage
		NOT_RESIZABLE = 0x4,        // can not accept size different than in XML file
		DRAWS_ALPHA = 0x8,          // wants to use alpha channel
		NO_TRANSPARENCY = 0x10,     // image is solid rectangle
		MASK_TRANSPARENCY = 0x20,   // use alpha channel to mask image
		DOUBLE_BUFFER = 0x40        // will draw in background, use double buffering
	};
};

// forward class declaration
class IPanelCCallback;

class IGaugeCCallback {
public:
	virtual ULONG AddRef() = 0;
	virtual ULONG Release() = 0;
	virtual IGaugeCCallback* QueryInterface(PCSTRINGZ pszInterface) = 0;
	virtual void Update() = 0;
	virtual bool GetPropertyValue(SINT32 id, FLOAT64* pValue) = 0;
	virtual bool GetPropertyValue(SINT32 id, PCSTRINGZ* pszValue) = 0;
	virtual bool SetPropertyValue(SINT32 id, FLOAT64 value) = 0;
	virtual bool SetPropertyValue(SINT32 id, PCSTRINGZ szValue) = 0;
	virtual IGaugeCDrawable* CreateGaugeCDrawable(SINT32 id, const IGaugeCDrawableCreateParameters* pParameters) = 0;
};

#define ISERIALIZABLE_GAUGECCALLBACK_NAME  "ISerializableGaugeCCallback"


class NetOut;
class NetIn;

class ISerializableGaugeCCallback : public IGaugeCCallback {
public:
	virtual bool Serialize(NetOut& netout) = 0;
	virtual bool Deserialize(NetIn& netin) = 0;
};

class IAircraftCCallback {
public:
	virtual ULONG AddRef() = 0;
	virtual ULONG Release() = 0;
	virtual IAircraftCCallback* QueryInterface(PCSTRINGZ pszInterface) = 0;
	virtual IGaugeCCallback* CreateGaugeCCallback() = 0;
	virtual void Update() = 0;
};

class IPanelCCallback {
public:
	virtual ULONG AddRef() = 0;
	virtual ULONG Release() = 0;
	virtual IPanelCCallback* QueryInterface(PCSTRINGZ pszInterface) = 0;
	virtual UINT32 GetVersion() = 0;
	virtual IAircraftCCallback*  CreateAircraftCCallback(UINT32 ContainerID) = 0;
	virtual bool  ConvertStringToProperty(PCSTRINGZ keyword, SINT32* pID) = 0;
	virtual bool  ConvertPropertyToString(SINT32 id, PPCSTRINGZ pKeyword) = 0;
	virtual bool  GetPropertyUnits(SINT32 id, ENUM* pEnum) = 0;            // UNITS_UNKNOWN if no units, UNITS_STRING if string
};

#define IFSX_PANELCCALLBACK_NAME "IFSXPanelCCallback"

class IFSXPanelCCallback : public IPanelCCallback
{
public:
	virtual void Clear() = 0; // clear all internal state and internal list of state variables (as GPS does)
};

#define DECLARE_PANEL_CALLBACK_REFCOUNT(CLASSNAME)  \
    private:                                        \
        ULONG   m_RefCount;                         \
    public:                                         \
        ULONG AddRef ();                            \
        ULONG Release ();                           \

#define DEFINE_PANEL_CALLBACK_REFCOUNT(CLASSNAME)   \
    ULONG CLASSNAME::AddRef ()                      \
    {                                               \
        return ++m_RefCount;                        \
    }                                               \
    ULONG CLASSNAME::Release ()                     \
    {                                               \
        ULONG result = --m_RefCount;                \
        if (result < 1)                             \
            delete this;                            \
        return result;                              \
    }

#define INIT_PANEL_CALLBACK_REFCOUNT(CLASSNAME)     \
    m_RefCount = 1;

typedef void(*GAUGE_KEY_EVENT_HANDLER) (ID32 event, UINT32 evdata, PVOID userdata);
typedef void(*GAUGE_KEY_EVENT_HANDLER_EX1) (ID32 event, UINT32 evdata0, UINT32 evdata1, UINT32 evdata2, UINT32 evdata3, UINT32 evdata4, PVOID userdata);


typedef enum {
	PANEL_TYPE_PLAIN,               //  Radio Stack
	PANEL_TYPE_HUD,                 //  Aircraft HUD
	PANEL_TYPE_SPECIAL,             //      LSO
	PANEL_TYPE_TEXTURE,             //  Virtual Cockpit Texture
	PANEL_TYPE_MISSION,             //  Context-specific (i.e. displayed during certain missions)

} PANEL_TYPE;


typedef FLOAT64 FSAPI FN_FLOAT64(FLOAT64       val);
typedef               FN_FLOAT64      *PFN_FLOAT64;

typedef FLOAT64 FSAPI MODULE_VAR_CB(PMODULE_VAR   val);
typedef               MODULE_VAR_CB   *PMODULE_VAR_CB;


// Seqsel Selection stuff
#define SELECT_NONE                             0
#define SELECT_1                                1
#define SELECT_ZOOM                             2
#define SELECT_MAGNETO                          3
#define SELECT_COM_WHOLE                        4
#define SELECT_COM_FRACTION                     5
#define SELECT_NAV1_WHOLE                       6
#define SELECT_NAV1_FRACTION                    7
#define SELECT_NAV2_WHOLE                       8
#define SELECT_NAV2_FRACTION                    9
#define SELECT_XPNDR_1000                       10
#define SELECT_XPNDR_0100                       11
#define SELECT_XPNDR_0010                       12
#define SELECT_XPNDR_0001                       13
#define SELECT_VOR1                             14
#define SELECT_VOR2                             15
#define SELECT_ENGINE                           16
#define SELECT_DME1                             17
#define SELECT_DME2                             18
#define SELECT_ADF_100                          19
#define SELECT_ADF_010                          20
#define SELECT_ADF_001                          21
#define SELECT_EGT_BUG                          22
#define SELECT_SIM_RATE                         23
#define SELECT_CLOCK_HOURS                      24
#define SELECT_CLOCK_MINUTES                    25
#define SELECT_CLOCK_SECONDS                    26
#define SELECT_COM2_WHOLE                       27
#define SELECT_COM2_FRACTION                    28
#define SELECT_ADF_TENTHS                       29

// used for the image_flags field
#define         IMAGE_USE_TRANSPARENCY                  BIT0    // these don't change dynamically and are set
#define         IMAGE_USE_ERASE                         BIT1
#define         IMAGE_USE_BRIGHT                        BIT2
#define         IMAGE_ERASE_ON_FAILURE                  BIT3    // not used now
#define         IMAGE_NO_STATIC_BLENDING                BIT4    // not used now
#define         IMAGE_CREATE_DIBSECTION                 BIT5
#define         IMAGE_BILINEAR_GRAY                     BIT6
#define         IMAGE_BILINEAR_COLOR                    BIT7
#define         IMAGE_PRESERVE_COLOR_IN_HUD             BIT8
#define         IMAGE_CONTAINS_NO_MASK                  BIT9

#define         IMAGE_SPRITE_FORCE_TRANS                BIT10   // not used now
#define         IMAGE_BLT_MASK_ONLY                     BIT11

#define         IMAGE_CONTAINS_MASK                     BIT12   // Image contains mask bits
#define         IMAGE_USE_ALPHA                         BIT13   // Image contains alpha channel
#define         IMAGE_USE_LUMINOUS                      BIT14   // Image is bright when the interior light is on
#define         IMAGE_USE_LUMINOUS_PARTIAL              BIT15       // Parts of image are lit by interior light (alpha channel)
#define			IMAGE_ERASE_ALWAYS						BIT16

#define         IMAGE_HIDDEN_TREE                       BIT25
#define         IMAGE_NO_STRETCH                        BIT27   // these change dynamically
#define         IMAGE_HUD_COLOR_MAP                     BIT28
#define         IMAGE_NO_TRANSLATION                    BIT29
#define         IMAGE_HIDDEN                            BIT30
#define         IMAGE_ON_SCREEN                         BIT31


#define         GET_IMAGE_HIDDEN(element)                       (element->image_flags & IMAGE_HIDDEN)
#define         SHOW_IMAGE(element)                             (element->image_flags &= ~IMAGE_HIDDEN)
#define         HIDE_IMAGE(element)                             (element->image_flags |= IMAGE_HIDDEN)

#define         GET_IMAGE_HIDDEN_TREE(element)                  (element->image_flags & IMAGE_HIDDEN_TREE)
#define         SHOW_IMAGE_TREE(element)                        (element->image_flags &= ~IMAGE_HIDDEN_TREE)
#define         HIDE_IMAGE_TREE(element)                        (element->image_flags |= IMAGE_HIDDEN_TREE)

#define         GET_USE_TRANSPARENCY( element )                 (element->image_flags & IMAGE_USE_TRANSPARENCY)
#define         GET_USE_ERASE( element )                        (element->image_flags & IMAGE_USE_ERASE)
#define         GET_USE_BRIGHT( element )                       (element->image_flags & IMAGE_USE_BRIGHT)
#define         GET_ERASE_ON_FAILURE( element )                 (element->image_flags & IMAGE_ERASE_ON_FAILURE)

#define         GET_ON_SCREEN( element )                        (element->image_flags & IMAGE_ON_SCREEN)
#define         SET_ON_SCREEN( element )                        (element->image_flags |= IMAGE_ON_SCREEN)
#define         SET_OFF_SCREEN( element )                       (element->image_flags &= ~IMAGE_ON_SCREEN)


#define         GET_FAILED( element )                           (element->image_flags & GAUGE_FAILURE)
#define         SET_GAUGE_FAILED( element)                      (element->image_flags |= GAUGE_FAILURE)
#define         SET_GAUGE_NOT_FAILED( element)                  (element->image_flags &= ~GAUGE_FAILURE)


typedef enum ELEMENT_TYPE_ENUM {
	ELEMENT_TYPE_STATIC_IMAGE,
	ELEMENT_TYPE_NEEDLE,
	ELEMENT_TYPE_STRING,
	ELEMENT_TYPE_SLIDER,
	ELEMENT_TYPE_ICON,
	ELEMENT_TYPE_MOVING_IMAGE,
	ELEMENT_TYPE_SPRITE,
}
ELEMENT_TYPE_ENUM;


#define ASI_ALT_MANUALLY_TUNABLE                BIT0
#define ASI_HEADING_MANUALLY_TUNABLE            BIT1


typedef struct NONLINEARITY
{
	PIXPOINT        pt;
	FLOAT64         value;
	FLOAT64         degrees;
}
NONLINEARITY, *PNONLINEARITY, **PPNONLINEARITY;

typedef enum    FAILURE_KEY
{
	FAIL_NONE = 0,

	OLD_FAIL_SYSTEM_ELECTRICAL,         // obsolete: dont use it in new gauges; use FAIL_SYSTEM_ELECTRICAL_PANELS
	FAIL_SYSTEM_ENGINE,
	FAIL_SYSTEM_PITOT_STATIC,
	FAIL_SYSTEM_VACUUM,

	FAIL_GAUGE_ADF,
	FAIL_GAUGE_AIRSPEED,
	FAIL_GAUGE_ALTIMETER,
	FAIL_GAUGE_ATTITUDE,
	FAIL_GAUGE_COMMUNICATIONS,              // Used for COM1
	FAIL_GAUGE_FUEL_INDICATORS,
	FAIL_GAUGE_GYRO_HEADING,
	FAIL_GAUGE_MAGNETIC_COMPASS,
	OLD_FAIL_GAUGE_NAVIGATION,              // OBSOLETE: DON'T USE
	FAIL_GAUGE_NAVIGATION_VOR1,             // only vor1
	FAIL_GAUGE_NAVIGATION_VOR2,             // only vor2
	OLD_FAIL_GAUGE_NAVIGATION_BOTH,         // OBSOLETE: DON'T USE
	FAIL_GAUGE_TRANSPONDER,
	FAIL_GAUGE_TURN_COORDINATOR,
	FAIL_GAUGE_VERTICAL_SPEED,

	FAIL_SYSTEM_ELECTRICAL_PANELS,
	FAIL_SYSTEM_ELECTRICAL_AVIONICS,

	FAIL_GAUGE_COMMUNICATIONS_COM2,        // Used for COM2


	FAIL_KEY_MAX
}
FAILURE_KEY, *PFAILURE_KEY, **PPFAILURE_KEY;

typedef enum    FAILURE_ACTION
{
	FAIL_ACTION_NONE = 0,
	FAIL_ACTION_FREEZE,
	FAIL_ACTION_ZERO,
	FAIL_ACTION_NO_DRAW,
	FAIL_ACTION_COVER,

	FAIL_ACTION_MAX
}
FAILURE_ACTION, *PFAILURE_ACTION, **PPFAILURE_ACTION;

typedef struct  FAILURE_RECORD
{
	FAILURE_KEY             key;
	FAILURE_ACTION          action;
	PVOID                   reserved;
}
FAILURE_RECORD, *PFAILURE_RECORD, **PPFAILURE_RECORD;

typedef struct  IMAGE_SET
{
	PIMAGE  final;
	PIMAGE  source;
	PIMAGE  reserved1;
	PIMAGE  reserved2;
} IMAGE_SET, *PIMAGE_SET, **PPIMAGE_SET;


#define PANEL_SERVICE_PRE_QUERY                         0
#define PANEL_SERVICE_POST_QUERY                        1
#define PANEL_SERVICE_PRE_INSTALL                       2       // extra_data = resource_handle
#define PANEL_SERVICE_POST_INSTALL                      3       // extra_data = resource_handle
#define PANEL_SERVICE_PRE_INITIALIZE                    4
#define PANEL_SERVICE_POST_INITIALIZE                   5
#define PANEL_SERVICE_PRE_UPDATE                        6
#define PANEL_SERVICE_POST_UPDATE                       7
#define PANEL_SERVICE_PRE_GENERATE                      8       // extra_data = phase
#define PANEL_SERVICE_POST_GENERATE                     9       // extra_data = phase
#define PANEL_SERVICE_PRE_DRAW                          10
#define PANEL_SERVICE_POST_DRAW                         11
#define PANEL_SERVICE_PRE_KILL                          12
#define PANEL_SERVICE_POST_KILL                         13
#define PANEL_SERVICE_CONNECT_TO_WINDOW                 14      // extra_data = PANEL_WND
#define PANEL_SERVICE_DISCONNECT                        15      // extra_data = PANEL_WND
#define PANEL_SERVICE_PANEL_OPEN                        16
#define PANEL_SERVICE_PANEL_CLOSE                       17


#define GAUGE_HEADER_VERSION_FS610                      610    // FS 6.10 gauges
#define GAUGE_HEADER_VERSION_FSNEW                      625    // OLD style gauges compiled in the FS70 TREE
#define GAUGE_HEADER_VERSION_CFS1                       650    // CFS1 Gauges
#define GAUGE_HEADER_VERSION_FS700                      700    // new FS7 gauges built after CFS1 is finalized
#define GAUGE_HEADER_VERSION_CFS2                       750    // gauges built after FS2000 shipped and before CFS2 shipped
#define GAUGE_HEADER_VERSION_FS800                      800    // FS2002 gauges 
#define GAUGE_HEADER_VERSION_FS900                      900    // FS2004 gauges
#define GAUGE_HEADER_VERSION_FS1000                     1000   // FSX gauges


typedef struct GAUGEHDR *PGAUGEHDR, **PPGAUGEHDR;
typedef struct ELEMENT_HEADER *PELEMENT_HEADER, **PPELEMENT_HEADER;
typedef struct MOUSERECT *PMOUSERECT, **PPMOUSERECT;

//
// gauge interface routines used by the panels system
//
//
// FS6.1, not used by FS7.0 gauges
typedef UINT32 GENERATE_PHASE;

typedef void FSAPI QUERY_ROUTINE(void);
typedef            QUERY_ROUTINE           *PQUERY_ROUTINE;

typedef void FSAPI INSTALL_ROUTINE(PVOID resource_file_handle);
typedef            INSTALL_ROUTINE         *PINSTALL_ROUTINE;

typedef void FSAPI INITIALIZE_ROUTINE(void);
typedef            INITIALIZE_ROUTINE       *PINITIALIZE_ROUTINE;

typedef void FSAPI UPDATE_ROUTINE(void);
typedef            UPDATE_ROUTINE          *PUPDATE_ROUTINE;

typedef void FSAPI GENERATE_ROUTINE(UINT32 phase);
typedef            GENERATE_ROUTINE        *PGENERATE_ROUTINE;

typedef void FSAPI DRAW_ROUTINE(void);
typedef            DRAW_ROUTINE            *PDRAW_ROUTINE;

typedef void FSAPI KILL_ROUTINE(void);
typedef            KILL_ROUTINE            *PKILL_ROUTINE;

typedef void FSAPI GAUGE_CALLBACK(PGAUGEHDR pgauge, SINT32 service_id, UINT32 extra_data);
typedef            GAUGE_CALLBACK  *PGAUGE_CALLBACK;

typedef void    SERIALIZE_SIZE_CALLBACK(PGAUGEHDR gauge_header, UINT32* size);
typedef         SERIALIZE_SIZE_CALLBACK *PSERIALIZE_SIZE_CALLBACK;

typedef void    SERIALIZE_CALLBACK(PGAUGEHDR gauge_header, BYTE* pBuf);
typedef         SERIALIZE_CALLBACK *PSERIALIZE_CALLBACK;

// return true if successful
typedef bool    DESERIALIZE_CALLBACK(PGAUGEHDR gauge_header, BYTE* pBuf);
typedef         DESERIALIZE_CALLBACK *PDESERIALIZE_CALLBACK;

typedef void    EVENT_SIZE_CALLBACK(PGAUGEHDR gauge_header, UINT32* size);
typedef         EVENT_SIZE_CALLBACK *PEVENT_SIZE_CALLBACK;

// return true if successful
typedef bool    PROCESS_EVENT_CALLBACK(PGAUGEHDR gauge_header, BYTE* pBuf);
typedef         PROCESS_EVENT_CALLBACK *PPROCESS_EVENT_CALLBACK;

typedef struct  GAUGEHDR
{
	UINT32                  gauge_header_version;
	char                    *gauge_name;
	PPELEMENT_HEADER        elements_list;
	PQUERY_ROUTINE          query_routine;
	PINSTALL_ROUTINE        install_routine;
	PINITIALIZE_ROUTINE     initialize_routine;
	PUPDATE_ROUTINE         update_routine;
	PGENERATE_ROUTINE       generate_routine;
	PDRAW_ROUTINE           draw_routine;
	PKILL_ROUTINE           kill_routine;
	PVOID                   reserved1;
	UINT32                  size_x_mm;
	UINT32                  size_y_mm;
	FLOAT32                 x_adjust;
	FLOAT32                 y_adjust;
	PVOID                   reserved2;
	PVOID                   reserved3;
	PIXPOINT                position;
	PVOID                   reserved4;
	PMOUSERECT              mouse_rect;
	PGAUGE_CALLBACK         gauge_callback;
	UINT32                  user_data;
	char*                   parameters;
	char*                   usage;
	SINT32                  reserved5;
	PVOID                   reserved6;
	PIXPOINT                size;
	FLOAT64                 user_area[10];
	FLAGS32                 flags;
	PVOID                   reserved7;
	GUID                         guid;
	UINT32                       key_id;
	PSERIALIZE_SIZE_CALLBACK     serialize_size_callback;
	PSERIALIZE_CALLBACK          serialize_callback;
	PDESERIALIZE_CALLBACK        deserialize_callback;
	PEVENT_SIZE_CALLBACK         event_size_callback;
	PPROCESS_EVENT_CALLBACK      process_event_callback;
	PIXPOINT                     original_position;
}
GAUGEHDR;


#define ELEMENT_INFO                                        \
    ELEMENT_TYPE_ENUM               element_type;           \
    ID                              resource_id;            \
    PIXPOINT                        position;               \
    PIXPOINT                        previous_position;      \
    PIXPOINT                        ofs;                    \
    PGAUGEHDR                       gauge_header;           \
    struct ELEMENT_HEADER           *previous_element;      \
    struct ELEMENT_HEADER           **next_element;         \
    PFAILURE_RECORD                 failure_systems;        \
    FLAGS                           image_flags;            \
    FLAGS                           aircraft_special_instrumentation;\
    FLAGS                           reserved;


typedef struct ELEMENT_HEADER
{
	ELEMENT_INFO
}
ELEMENT_HEADER;


#define HEADER union        \
{                           \
    struct                  \
    {                       \
        ELEMENT_INFO        \
    };                      \
    ELEMENT_HEADER header;  \
}


typedef struct ELEMENT_STATIC_IMAGE *PELEMENT_STATIC_IMAGE, **PPELEMENT_STATIC_IMAGE;
typedef struct ELEMENT_NEEDLE       *PELEMENT_NEEDLE, **PPELEMENT_NEEDLE;
typedef struct ELEMENT_STRING           *PELEMENT_STRING, **PPELEMENT_STRING;
typedef struct ELEMENT_SLIDER           *PELEMENT_SLIDER, **PPELEMENT_SLIDER;
typedef struct ELEMENT_ICON                     *PELEMENT_ICON, **PPELEMENT_ICON;
typedef struct ELEMENT_MOVING_IMAGE     *PELEMENT_MOVING_IMAGE, **PPELEMENT_MOVING_IMAGE;
typedef struct ELEMENT_SPRITE           *PELEMENT_SPRITE, **PPELEMENT_SPRITE;

typedef FLOAT64 FSAPI NEEDLE_UPDATE_CALLBACK(PELEMENT_NEEDLE pelement);
typedef               NEEDLE_UPDATE_CALLBACK                  *PNEEDLE_UPDATE_CALLBACK;

typedef FLOAT64 FSAPI STRING_UPDATE_CALLBACK(PELEMENT_STRING pelement);
typedef               STRING_UPDATE_CALLBACK                  *PSTRING_UPDATE_CALLBACK;

typedef FLOAT64 FSAPI SLIDER_UPDATE_CALLBACK(PELEMENT_SLIDER pelement);
typedef               SLIDER_UPDATE_CALLBACK                  *PSLIDER_UPDATE_CALLBACK;

typedef FLOAT64 FSAPI ICON_UPDATE_CALLBACK(PELEMENT_ICON pelement);
typedef               ICON_UPDATE_CALLBACK                    *PICON_UPDATE_CALLBACK;

typedef FLOAT64 FSAPI MOVING_IMAGE_UPDATE_CALLBACK(PELEMENT_MOVING_IMAGE pelement);
typedef               MOVING_IMAGE_UPDATE_CALLBACK    *PMOVING_IMAGE_UPDATE_CALLBACK;

typedef FLOAT64 FSAPI SPRITE_UPDATE_CALLBACK(PELEMENT_SPRITE pelement);
typedef               SPRITE_UPDATE_CALLBACK                  *PSPRITE_UPDATE_CALLBACK;



typedef struct ELEMENT_STATIC_IMAGE
{
	HEADER;
	IMAGE_SET               image_data;
	PIMAGE                  save_buffer;
	HDC                     hdc;                                    // HDC for Win32 API DrawText
	HBITMAP                 hbmp;                                   // DibSection for Win32 API DrawText
	HBITMAP                 reserved1;
	PIMAGE                  reserved2;
}
ELEMENT_STATIC_IMAGE, *PELEMENT_STATIC_IMAGE, **PPELEMENT_STATIC_IMAGE;

#define MAKE_STATIC(    NAME,                                                       \
                        RES_ID,                                                     \
                        NEXT_LIST,                                                  \
                        FAILURE,                                                    \
                        DRAW_FLAGS,                                                 \
                        ASI_FLAGS,                                                  \
                        POSITION_X, POSITION_Y )                                    \
                                                                                    \
    ELEMENT_STATIC_IMAGE        NAME    =                                           \
    {                                                                               \
        ELEMENT_TYPE_STATIC_IMAGE,                                                  \
        RES_ID,                                                                     \
        POSITION_X, POSITION_Y,                                                     \
        {0,0},                                                                      \
        {0,0},                                                                      \
        &GAUGEHDR_VAR_NAME,                                                         \
        NULL,                                                                       \
        (PPELEMENT_HEADER)(NEXT_LIST),                                              \
        (PFAILURE_RECORD)(FAILURE),                                                 \
        DRAW_FLAGS,                                                                 \
        ASI_FLAGS,                                                                  \
        0                                                                           \
    };



typedef struct ELEMENT_NEEDLE
{
	HEADER;
	MODULE_VAR              source_var;
	PMODULE_VAR_CB          source_var_cb;
	PFN_FLOAT64             display_value_fn;
	PFN_FLOAT64             angle_fn;
	PFN_FLOAT64             lag_fn;
	PIXPOINT                reference;
	PNONLINEARITY           nonlinearity_table;
	UINT32                  nonlinearity_table_size;
	SINT32                  max_degrees_per_tick;
	FLOAT64                 readout_goal;
	FLOAT64                 theta_goal;
	FLOAT64                 theta;
	PIXPOINT                reference_old;
	PIXPOINT                o0, o1, o2, o3;
	IMAGE_SET               needle_data;
	PIMAGE                  texture_data;
	UINT32                  texture_handle;
	PIMAGE                  save_buffer;
	PIXPOINT                save_position;
	PNEEDLE_UPDATE_CALLBACK update_cb;
	PIXPOINT                save_size;
}
ELEMENT_NEEDLE, *PELEMENT_NEEDLE, **PPELEMENT_NEEDLE;

#define MAKE_NEEDLE(    NAME,                                                       \
                        RES_ID,                                                     \
                        NEXT_LIST,                                                  \
                        FAILURE,                                                    \
                        DRAW_FLAGS,                                                 \
                        ASI_FLAGS,                                                  \
                        BKND_POSITION_X, BKND_POSITION_Y,                           \
                        NDL_POSITION_X, NDL_POSITION_Y,                             \
                        SOURCE_VAR, CALLBACK,                                       \
                        NONLINEARITY_TABLE,                                         \
                        MAX_DEG_PER_SEC )                                           \
                                                                                    \
    ELEMENT_NEEDLE              NAME    =                                           \
    {                                                                               \
        ELEMENT_TYPE_NEEDLE,                                                        \
        RES_ID,                                                                     \
        BKND_POSITION_X, BKND_POSITION_Y,                                           \
        {0,0},                                                                      \
        NDL_POSITION_X, NDL_POSITION_Y,                                             \
        &GAUGEHDR_VAR_NAME,                                                         \
        NULL,                                                                       \
        (PPELEMENT_HEADER)(NEXT_LIST),                                              \
        (PFAILURE_RECORD)(FAILURE),                                                 \
        DRAW_FLAGS,                                                                 \
        ASI_FLAGS,                                                                  \
        0,                                                                          \
                                                                                    \
        {SOURCE_VAR},                                                               \
        NULL,                                                                       \
        NULL,                                                                       \
        NULL,                                                                       \
        NULL,                                                                       \
        0, 0,                                                                       \
        NONLINEARITY_TABLE,                                                         \
        sizeof( NONLINEARITY_TABLE )/sizeof( NONLINEARITY ),                        \
        MAX_DEG_PER_SEC,                                                            \
        0.0,                                                                        \
        0.0,                                                                        \
        0.0,                                                                        \
        {0,0},                                                                      \
        {0,0},{0,0},{0,0},{0,0},                                                    \
        {0},                                                                        \
        NULL,                                                                       \
        0,                                                                          \
        0,                                                                          \
        {0,0},                                                                      \
        CALLBACK,                                                                   \
        {0,0},                                                                      \
    };



typedef struct SEQ_REC
{
	SINT32 seq_id;
	SINT32 sel_str;
	SINT32 sel_end;
}
SEQ_REC, *PSEQ_REC, **PPSEQ_REC;

#define STR_UNSEL(ELEMENT)                      ((ELEMENT)->sel_end = -1, (ELEMENT)->sel_str = -1)
#define STR_SEL(ELEMENT, STR, END)              ((ELEMENT)->sel_end = (END), (ELEMENT)->sel_str = (STR))
#define IS_STR_SEL(ELEMENT)                     ((ELEMENT)->sel_end != -1 && (ELEMENT)->sel_str != -1)

typedef struct ELEMENT_STRING
{
	HEADER;
	MODULE_VAR              source_var[3];
	FLOAT64                 save_var[3];
	PMODULE_VAR_CB          source_var_cb[3];
	PFN_FLOAT64             source_fn;
	PCHAR                   string;
	PCHAR                   string_old;
	PIXPOINT                img_size;
	PIMAGE                  string_data;
	PIMAGE                  save_buffer;
	COLORREF                fg_color;
	COLORREF                bg_color;
	COLORREF                hilite_color;
	HDC                     hdc;
	HFONT                   hfont;
	HFONT                   reserved1;
	HBITMAP                 hbmp;
	HBITMAP                 reserved2;
	HBRUSH                  hbrush;
	HBRUSH                  reserved3;
	char                    font_name[32];
	SINT32                  font_weight;
	UINT8                   font_charset;
	UINT32                  draw_text_flags;
	UINT16                  max_string_length;
	UINT16                  char_width;
	SINT16                  sel_str;
	SINT16                  sel_end;
	PSEQ_REC                seq;
	SINT32                  save_global_seq;
	SINT32                  char_height;
	PSTRING_UPDATE_CALLBACK update_cb;
	BOOL                    use_user_color;
	COLORREF                user_color;
}
ELEMENT_STRING, *PELEMENT_STRING, **PPELEMENT_STRING;

#define MAKE_STRING(    NAME,                                                       \
                        NEXT_LIST,                                                  \
                        FAILURE,                                                    \
                        DRAW_FLAGS,                                                 \
                        ASI_FLAGS,                                                  \
                        POSITION_X, POSITION_Y,                                     \
                        SIZE_X, SIZE_Y,                                             \
                        NUM_CHARS,                                                  \
                        SOURCE_VAR_1,                                               \
                        SOURCE_VAR_2,                                               \
                        SOURCE_VAR_3,                                               \
                        FORECOLOR,                                                  \
                        BACKCOLOR,                                                  \
                        HILITECOLOR,                                                \
                        FONT_NAME,                                                  \
                        FONT_WEIGHT,                                                \
                        FONT_CHARSET,                                               \
                        FONT_SIZE,                                                  \
                        DRAW_TEXT_FLAGS,                                            \
                        HILITE_LIST,                                                \
                        CALLBACK)                                                   \
                                                                                    \
    ELEMENT_STRING              NAME    =                                           \
    {                                                                               \
        ELEMENT_TYPE_STRING,                                                        \
        -1,                                                                         \
        POSITION_X, POSITION_Y,                                                     \
        {0,0},                                                                      \
        {0,0},                                                                      \
        &GAUGEHDR_VAR_NAME,                                                         \
        NULL,                                                                       \
        (PPELEMENT_HEADER)(NEXT_LIST),                                              \
        (PFAILURE_RECORD)(FAILURE),                                                 \
        DRAW_FLAGS,                                                                 \
        ASI_FLAGS,                                                                  \
        0,                                                                          \
                                                                                    \
        {{SOURCE_VAR_1}, {SOURCE_VAR_2}, {SOURCE_VAR_3}},                           \
        {0, 0, 0},                                                                  \
        {NULL, NULL, NULL},                                                         \
        NULL,                                                                       \
        NULL,                                                                       \
        NULL,                                                                       \
        SIZE_X, SIZE_Y,                                                             \
        NULL,                                                                       \
        NULL,                                                                       \
        FORECOLOR,                                                                  \
        BACKCOLOR,                                                                  \
        HILITECOLOR,                                                                \
        0,                                                                          \
        0,                                                                          \
        0,                                                                          \
        0,                                                                          \
        0,                                                                          \
        0,                                                                          \
        0,                                                                          \
        FONT_NAME,                                                                  \
        FONT_WEIGHT,                                                                \
        FONT_CHARSET,                                                               \
        DRAW_TEXT_FLAGS,                                                            \
        NUM_CHARS,                                                                  \
        0,                                                                          \
        0,                                                                          \
        0,                                                                          \
        HILITE_LIST,                                                                \
        0,                                                                          \
        FONT_SIZE,                                                                  \
        CALLBACK,                                                                   \
        FALSE,                                                                      \
        0,                                                                          \
    };


typedef struct ELEMENT_SLIDER
{
	HEADER;
	MODULE_VAR              source_var_x;
	PMODULE_VAR_CB          source_var_x_cb;
	FLOAT64                 scale_x;
	PFN_FLOAT64             lag_fn_x;
	MODULE_VAR              source_var_y;
	PMODULE_VAR_CB          source_var_y_cb;
	FLOAT64                 scale_y;
	PFN_FLOAT64             lag_fn_y;
	IMAGE_SET               slider_data;
	PIMAGE                  save_buffer;
	PSLIDER_UPDATE_CALLBACK update_x_cb;
	PSLIDER_UPDATE_CALLBACK update_y_cb;
	FLOAT64 previous_x;
	FLOAT64 previous_y;
}
ELEMENT_SLIDER, *PELEMENT_SLIDER, **PPELEMENT_SLIDER;

#define MAKE_SLIDER(    NAME,                                                       \
                        RES_ID,                                                     \
                        NEXT_LIST,                                                  \
                        FAILURE,                                                    \
                        DRAW_FLAGS,                                                 \
                        ASI_FLAGS,                                                  \
                        POSITION_X, POSITION_Y,                                     \
                        SOURCE_VAR_X, CALLBACK_X, SCALE_X,                          \
                        SOURCE_VAR_Y, CALLBACK_Y, SCALE_Y )                         \
                                                                                    \
    ELEMENT_SLIDER              NAME    =                                           \
    {                                                                               \
        ELEMENT_TYPE_SLIDER,                                                        \
        RES_ID,                                                                     \
        POSITION_X, POSITION_Y,                                                     \
        {0,0},                                                                      \
        {0,0},                                                                      \
        &GAUGEHDR_VAR_NAME,                                                         \
        NULL,                                                                       \
        (PPELEMENT_HEADER)(NEXT_LIST),                                              \
        (PFAILURE_RECORD)(FAILURE),                                                 \
        DRAW_FLAGS,                                                                 \
        ASI_FLAGS,                                                                  \
        0,                                                                          \
                                                                                    \
        {SOURCE_VAR_X},                                                             \
        NULL,                                                                       \
        SCALE_X,                                                                    \
        NULL,                                                                       \
        {SOURCE_VAR_Y},                                                             \
        NULL,                                                                       \
        SCALE_Y,                                                                    \
        NULL,                                                                       \
        {0},                                                                        \
        NULL,                                                                       \
        CALLBACK_X,                                                                 \
        CALLBACK_Y                                                                  \
    };



// Icon Switch Types
#define ICON_SWITCH_TYPE_SET_CUR_ICON                   0
#define ICON_SWITCH_TYPE_SET_CUR_USING_RANGE            1
#define ICON_SWITCH_TYPE_STEP_TO                        3
#define ICON_SWITCH_TYPE_STEP_TO_USING_RANGE            4


typedef struct ELEMENT_ICON
{
	HEADER;
	MODULE_VAR              source_var;
	PMODULE_VAR_CB          source_var_cb;
	SINT32                  switch_type;
	SINT32                  switch_count;
	FLOAT64                 scale;
	SINT32                  offset;
	SINT32                  num_icons;
	SINT32                  cur_icon;
	SINT32                  dst_icon;
	PIMAGE_SET              picon_data;
	PIMAGE                  save_buffer;
	PICON_UPDATE_CALLBACK   update_cb;
	ID                      reserved1;
}
ELEMENT_ICON, *PELEMENT_ICON, **PPELEMENT_ICON;

#define MAKE_ICON(      NAME,                                                       \
                        RES_ID,                                                     \
                        NEXT_LIST,                                                  \
                        FAILURE,                                                    \
                        DRAW_FLAGS,                                                 \
                        ASI_FLAGS,                                                  \
                        POSITION_X, POSITION_Y,                                     \
                        SOURCE_VAR, CALLBACK,                                       \
                        SWITCH_TYPE,                                                \
                        NUM_ICONS,                                                  \
                        SCALE,                                                      \
                        OFFSET )                                                    \
                                                                                    \
    ELEMENT_ICON                NAME    =                                           \
    {                                                                               \
        ELEMENT_TYPE_ICON,                                                          \
        RES_ID,                                                                     \
        POSITION_X, POSITION_Y,                                                     \
        {0,0},                                                                      \
        {0,0},                                                                      \
        &GAUGEHDR_VAR_NAME,                                                         \
        NULL,                                                                       \
        (PPELEMENT_HEADER)(NEXT_LIST),                                              \
        (PFAILURE_RECORD)(FAILURE),                                                 \
        DRAW_FLAGS,                                                                 \
        ASI_FLAGS,                                                                  \
        0,                                                                          \
                                                                                    \
        {SOURCE_VAR},                                                               \
        NULL,                                                                       \
        SWITCH_TYPE,                                                                \
        0,                                                                          \
        SCALE,                                                                      \
        OFFSET,                                                                     \
        NUM_ICONS,                                                                  \
        0,                                                                          \
        0,                                                                          \
        NULL,                                                                       \
        NULL,                                                                       \
        CALLBACK,                                                                   \
    };



typedef struct ELEMENT_MOVING_IMAGE
{
	HEADER;
	MODULE_VAR          source_var_x;
	PMODULE_VAR_CB      source_var_x_cb;
	FLOAT64             min_x;
	FLOAT64             max_x;
	PFN_FLOAT64         lag_x;
	MODULE_VAR          source_var_y;
	PMODULE_VAR_CB      source_var_y_cb;
	FLOAT64             min_y;
	FLOAT64             max_y;
	PFN_FLOAT64         lag_y;
	FLOAT64             scale_x;
	FLOAT64             scale_y;
	FLOAT64             offset_x;
	FLOAT64             offset_y;
	PIXPOINT            save_position;
	IMAGE_SET           image_data;
	IMAGE_SET           local_mask_data;
	PIMAGE              mask_data;
	PIMAGE              save_buffer;
	PMOVING_IMAGE_UPDATE_CALLBACK update_x_cb;
	PMOVING_IMAGE_UPDATE_CALLBACK update_y_cb;
	FLOAT64             previous_x;
	FLOAT64             previous_y;
}
ELEMENT_MOVING_IMAGE, *PELEMENT_MOVING_IMAGE, **PPELEMENT_MOVING_IMAGE;

#define MAKE_MOVING(    NAME,                                                       \
                        RES_ID,                                                     \
                        NEXT_LIST,                                                  \
                        FAILURE,                                                    \
                        DRAW_FLAGS,                                                 \
                        ASI_FLAGS,                                                  \
                        POSITION_X, POSITION_Y,                                     \
                        SOURCE_VAR_X, CALLBACK_X,                                   \
                        MIN_X, MAX_X,                                               \
                        SOURCE_VAR_Y, CALLBACK_Y,                                   \
                        MIN_Y, MAX_Y )                                              \
                                                                                    \
    ELEMENT_MOVING_IMAGE        NAME    =                                           \
    {                                                                               \
        ELEMENT_TYPE_MOVING_IMAGE,                                                  \
        RES_ID,                                                                     \
        POSITION_X, POSITION_Y,                                                     \
        {0,0},                                                                      \
        {0,0},                                                                      \
        &GAUGEHDR_VAR_NAME,                                                         \
        NULL,                                                                       \
        (PPELEMENT_HEADER)(NEXT_LIST),                                              \
        (PFAILURE_RECORD)(FAILURE),                                                 \
        DRAW_FLAGS,                                                                 \
        ASI_FLAGS,                                                                  \
        0,                                                                          \
                                                                                    \
        {SOURCE_VAR_X},                                                             \
        NULL,                                                                       \
        MIN_X,                                                                      \
        MAX_X,                                                                      \
        NULL,                                                                       \
        {SOURCE_VAR_Y},                                                             \
        NULL,                                                                       \
        MIN_Y,                                                                      \
        MAX_Y,                                                                      \
        NULL,                                                                       \
        0.0,                                                                        \
        0.0,                                                                        \
        0.0,                                                                        \
        0.0,                                                                        \
        {0,0},                                                                      \
        {0},                                                                        \
        {0},                                                                        \
        NULL,                                                                       \
        NULL,                                                                       \
        CALLBACK_X,                                                                 \
        CALLBACK_Y                                                                  \
    };



typedef struct ELEMENT_SPRITE
{
	HEADER;
	MODULE_VAR              source_var_x;
	PMODULE_VAR_CB          source_var_x_cb;
	FLOAT64                 scale_x;
	MODULE_VAR              source_var_y;
	PMODULE_VAR_CB          source_var_y_cb;
	FLOAT64                 scale_y;
	MODULE_VAR              source_var_0;
	PMODULE_VAR_CB          source_var_0_cb;
	FLOAT64                 scale_0;
	FLOAT32                 texture_scale_x;
	FLOAT32                 texture_scale_y;
	PIXPOINT                reference;
	PIXPOINT                reference_old;
	PIXPOINT                o0, o1, o2, o3;
	PIXPOINT                os0, os1, os2, os3;
	IMAGE_SET               sprite_data;
	PIMAGE                  texture_data;
	UINT32                  texture_handle;
	IMAGE_SET               local_mask_data;
	PIMAGE                  mask_data;
	PIMAGE                  save_buffer;
	PSPRITE_UPDATE_CALLBACK update_x_cb;
	PSPRITE_UPDATE_CALLBACK update_y_cb;
	PSPRITE_UPDATE_CALLBACK update_0_cb;
	FLOAT64                 previous_x;
	FLOAT64                 previous_y;
	FLOAT64                 previous_0;
}
ELEMENT_SPRITE, *PELEMENT_SPRITE, **PPELEMENT_SPRITE;

#define MAKE_SPRITE(    NAME,                                                       \
                        RES_ID,                                                     \
                        NEXT_LIST,                                                  \
                        FAILURE,                                                    \
                        DRAW_FLAGS,                                                 \
                        ASI_FLAGS,                                                  \
                        BKND_POSITION_X, BKND_POSITION_Y,                           \
                        TEXTURE_CENTER_X, TEXTURE_CENTER_Y,                         \
                        TEXTURE_SCALE_X, TEXTURE_SCALE_Y,                           \
                        SOURCE_VAR_X, CALLBACK_X, SCALE_X,                          \
                        SOURCE_VAR_Y, CALLBACK_Y, SCALE_Y,                          \
                        SOURCE_VAR_0, CALLBACK_0, SCALE_0 )                         \
                                                                                    \
    ELEMENT_SPRITE              NAME    =                                           \
    {                                                                               \
        ELEMENT_TYPE_SPRITE,                                                        \
        RES_ID,                                                                     \
        {BKND_POSITION_X, BKND_POSITION_Y},                                         \
        {0,0},                                                                      \
        {TEXTURE_CENTER_X, TEXTURE_CENTER_Y},                                       \
        &GAUGEHDR_VAR_NAME,                                                         \
        NULL,                                                                       \
        (PPELEMENT_HEADER)(NEXT_LIST),                                              \
        (PFAILURE_RECORD)(FAILURE),                                                 \
        DRAW_FLAGS,                                                                 \
        ASI_FLAGS,                                                                  \
        0,                                                                          \
                                                                                    \
        {SOURCE_VAR_X},                                                             \
        NULL,                                                                       \
        SCALE_X,                                                                    \
        {SOURCE_VAR_Y},                                                             \
        NULL,                                                                       \
        SCALE_Y,                                                                    \
        {SOURCE_VAR_0},                                                             \
        NULL,                                                                       \
        SCALE_0,                                                                    \
        (FLOAT32)TEXTURE_SCALE_X, (FLOAT32)TEXTURE_SCALE_Y,                         \
        {0,0},                                                                      \
        {0,0},                                                                      \
        {0,0},{0,0},{0,0},{0,0},                                                    \
        {0,0},{0,0},{0,0},{0,0},                                                    \
        {0},                                                                        \
        NULL,                                                                       \
        0,                                                                          \
        {0},                                                                        \
        NULL,                                                                       \
        NULL,                                                                       \
        CALLBACK_X,                                                                 \
        CALLBACK_Y,                                                                 \
        CALLBACK_0                                                                  \
    };



// defines for mouse sense rect stuff
typedef enum
{
	MOUSE_RECT_EOL,
	MOUSE_RECT_PARENT,
	MOUSE_RECT_CHILD,
	MOUSE_RECT_END_PARENT,
	MOUSE_RECT_USER,
} MOUSE_RECT_TYPE;

typedef enum
{
	CURSOR_NONE = -1,
	CURSOR_NORMAL = 0,
	CURSOR_UPARROW,
	CURSOR_DOWNARROW,
	CURSOR_LEFTARROW,
	CURSOR_RIGHTARROW,
	CURSOR_HAND,
	CURSOR_CROSSHAIR,
	CURSOR_GRAB,
} CURSOR_TYPE;


typedef struct  MOUSECALLBACK
{
	PIXPOINT    relative_point;
	PVOID       user_data;
	PMOUSERECT  mouse;
	PIXPOINT    screen_point;       // window client coordinates of the click
	PVOID       reserved;
} MOUSECALLBACK, *PMOUSECALLBACK, **PPMOUSECALLBACK;

// arg1 is actually pointer to MOUSECALLBACK, but for compatibility is defined here as a pointer to PIXPOINT
typedef BOOL FSAPI  MOUSE_FUNCTION(PPIXPOINT arg1, FLAGS32 mouse_flags);
typedef             MOUSE_FUNCTION      *PMOUSE_FUNCTION;

#define GAUGEHDR_FOR_MOUSE_CALLBACK(PIXPOINT) ((PGAUGEHDR)(((PMOUSECALLBACK)(PIXPOINT))->user_data))

typedef struct  MOUSERECT
{
	MOUSE_RECT_TYPE rect_type;          // type of mouse rectangle (parent, child, eol)
	PIXBOX          relative_box;       // relative sense rectangle (relative to parameter to register function)
	CURSOR_TYPE     cursor;             // cursor to display when over this window
	ID              help_id;            // pop-up help id
	FLAGS           mouse_flags;        // types of mouse activities to look for
	ID              event_id;           // event to generate if mouse_flags is satisfied (implies simple mouse_flags)
	PMOUSE_FUNCTION mouse_function;     // function to call if mouse_flag is satisfied
	PVOID           api_data;           // data for FS6API's use
} MOUSERECT;


#define HELP_NONE           0

// Mouse rectangle macros

#define MOUSE_MAXIMUM_TOOLTIP_TEXT 80

#define MOUSE_PARENT_BEGIN( x, y, w, h, helpid )                        {MOUSE_RECT_PARENT, {x, y, w, h}, CURSOR_NONE, helpid, MOUSE_NONE, 0, NULL, NULL},

#define MOUSE_PARENT_END                                                {MOUSE_RECT_END_PARENT, {0, 0, 0, 0}, CURSOR_NONE, HELP_NONE, MOUSE_NONE, 0, NULL, NULL},

#define MOUSE_PARENT( x, y, w, h, helpid )                              MOUSE_PARENT_BEGIN( x, y, w, h, helpid )                    \
                                                                        MOUSE_PARENT_END

#define MOUSE_BEGIN( name, helpid, x, y )                               MOUSERECT   name[]  = {                                     \
                                                                        MOUSE_PARENT_BEGIN( x, y, 0, 0, helpid )

#define MOUSE_END                                                       MOUSE_PARENT_END                                            \
                                                                        {MOUSE_RECT_EOL, {0, 0, 0, 0}, CURSOR_NONE, HELP_NONE, MOUSE_NONE, 0, NULL, NULL}};

#define MOUSE_CHILD_EVENT( x, y, w, h, cursor, mouse_flags, event_id )  {MOUSE_RECT_CHILD, {x, y, w, h}, cursor, HELP_NONE, mouse_flags, event_id, NULL, NULL},
#define MOUSE_CHILD_FUNCT( x, y, w, h, cursor, mouse_flags, function )  {MOUSE_RECT_CHILD, {x, y, w, h}, cursor, HELP_NONE, mouse_flags, 0, function, NULL},





// Dynamic tooltips' stuff

#define MOUSE_TOOLTIP_TEXT_MACRO(HELPID, HELPID_METRIC, HELPID_US, STRING, ARGS, NARG)  \
                            {MOUSE_RECT_USER, {0, 0, (HELPID_METRIC), (HELPID_US)}, CURSOR_NONE, (HELPID), MOUSE_NONE, (ID)(NARG), (BOOL (FSAPI *)(PPIXPOINT , FLAGS32))(PVOID)(STRING), (ARGS)}
#define MOUSE_TOOLTIP_TEXT(HELPID, HELPID_METRIC, HELPID_US, STRING, ARGS, NARG)    MOUSE_TOOLTIP_TEXT_MACRO(HELPID, HELPID_METRIC, HELPID_US, STRING, ARGS, NARG),
#define MOUSE_TOOLTIP_TEXT_3ID_NARG(HELPID, HELPID_METRIC, HELPID_US, ARGS, NARG)   MOUSE_TOOLTIP_TEXT (HELPID, HELPID_METRIC, HELPID_US, NULL, ARGS, NARG)
#define MOUSE_TOOLTIP_TEXT_ID_NARG(HELPID, ARGS, NARG)                              MOUSE_TOOLTIP_TEXT_3ID_NARG ((HELPID), HELP_NONE, HELP_NONE, (ARGS), (NARG))
#define MOUSE_TOOLTIP_TEXT_STRING_NARG(STRING, ARGS, NARG)                          MOUSE_TOOLTIP_TEXT (HELP_NONE, HELP_NONE, HELP_NONE, STRING, ARGS, NARG)
#define MOUSE_TOOLTIP_TEXT_ID(HELPID, ARGS)                                         MOUSE_TOOLTIP_TEXT_ID_NARG ((HELPID), (ARGS), sizeof(ARGS)/sizeof(MOUSE_ARG))
#define MOUSE_TOOLTIP_TEXT_STRING(STRING, ARGS)                                     MOUSE_TOOLTIP_TEXT_STRING_NARG ((STRING), (ARGS), sizeof(ARGS)/sizeof(MOUSE_ARG))
#define MOUSE_TOOLTIP_STRING(STRING)                                                MOUSE_TOOLTIP_TEXT_STRING_NARG ((STRING), NULL, 0)
#define MOUSE_TOOLTIP_ID(HELPID)                                                    MOUSE_TOOLTIP_TEXT_ID_NARG ((HELPID), NULL, 0)
#define MOUSE_TOOLTIP_3ID(HELPID, HELPID_METRIC, HELPID_US)                         MOUSE_TOOLTIP_TEXT_3ID_NARG ((HELPID), (HELPID_METRIC), (HELPID_US), NULL, 0)
#define MOUSE_TOOLTIP_STANDARD_STRING(STRING)                                       MOUSE_TOOLTIP_TEXT_STRING (STRING, 0)

#define MAKE_MOUSE_TOOLTIP_STANDARD_STRING(PARAMETER, UNITS)                        "@" ## #PARAMETER ## "@" ## #UNITS
#define MOUSE_TOOLTIP_STANDARD(PARAMETER, UNITS)                                    MOUSE_TOOLTIP_STANDARD_STRING (MAKE_MOUSE_TOOLTIP_STANDARD_STRING (PARAMETER, UNITS))


typedef struct {
	FLOAT64 source;
	FLOAT64 value;
} MOUSE_ARG_NUMERIC_MAP;
typedef struct {
	SINT32 source;
	ID value;
} MOUSE_ARG_ID_MAP;
typedef struct {
	SINT32 source;
	PSTRINGZ value;
} MOUSE_ARG_STRING_MAP;

typedef struct {
	MODULE_VAR      source_var;
	FLOAT64         scale;
	MOUSE_ARG_NUMERIC_MAP *numeric_table;
	UINT            numeric_table_size;
	MOUSE_ARG_ID_MAP *id_table;
	UINT            id_table_size;
	MOUSE_ARG_STRING_MAP *string_table;
	UINT            string_table_size;
	FLOAT64(FSAPI *numeric_callback) (FLOAT64 number, ID id, PCSTRINGZ string, MODULE_VAR *source_var, PGAUGEHDR gauge);
	ID(FSAPI *id_callback) (FLOAT64 number, ID id, PCSTRINGZ string, MODULE_VAR *source_var, PGAUGEHDR gauge);
	PCSTRINGZ(FSAPI *string_callback) (FLOAT64 number, ID id, PCSTRINGZ string, MODULE_VAR *source_var, PGAUGEHDR gauge);
} MOUSE_ARG;

#define MOUSE_TOOLTIP_ARGS(NAME)        MOUSE_ARG (NAME)[] = {
#define MOUSE_TOOLTIP_ARG(VAR, SCALE, NUMERIC_TABLE, ID_TABLE, STRING_TABLE, NUMERIC_CALLBACK, ID_CALLBACK, STRING_CALLBACK) \
                {{(VAR)}, (SCALE), \
                 (NUMERIC_TABLE), sizeof(NUMERIC_TABLE)/sizeof(MOUSE_ARG_NUMERIC_MAP), \
                 (ID_TABLE), sizeof(ID_TABLE)/sizeof(MOUSE_ARG_ID_MAP), \
                 (STRING_TABLE), sizeof(STRING_TABLE)/sizeof(MOUSE_ARG_STRING_MAP), \
                 (NUMERIC_CALLBACK), (ID_CALLBACK), (STRING_CALLBACK)},
#define MOUSE_TOOLTIP_ARG_NUMBER(VAR, SCALE) MOUSE_TOOLTIP_ARG((VAR), (SCALE), NULL, NULL, NULL, NULL, NULL, NULL)
#define MOUSE_TOOLTIP_ARG_HELP_ID(VAR, ID_TABLE) MOUSE_TOOLTIP_ARG((VAR), 1, NULL, (ID_TABLE), NULL, NULL, NULL, NULL)
#define MOUSE_TOOLTIP_ARG_NUMBER_FUNCT(VAR, FUNCT) MOUSE_TOOLTIP_ARG((VAR), 1, NULL, NULL, NULL, (FUNCT), NULL, NULL)
#define MOUSE_TOOLTIP_ARG_HELP_ID_FUNCT(VAR, FUNCT) MOUSE_TOOLTIP_ARG((VAR), 1, NULL, NULL, NULL, NULL, (FUNCT), NULL)
#define MOUSE_TOOLTIP_ARGS_END          };


#define FS9LINK_VERSION     0x0900


#define FSLINKAGE       ID          ModuleID;                       \
                        void        (FSAPI *ModuleInit)(void);      \
                        void        (FSAPI *ModuleDeinit)(void);    \
                        FLAGS32     ModuleFlags;                    \
                        UINT32      ModulePriority;                 \
                        UINT32      ModuleVersion;

// <ASOBO-MOD>
extern "C"
{
	[[deprecated("Panels API is now deprecated")]]
	BOOL FSAPI is_panel_window_visible_ident(UINT32 panel_id);

	[[deprecated("Panels API is now deprecated")]]
	ENUM FSAPI tooltip_units_getset(int action, ENUM type);  // action < 0 - toggle(ignore 'type'), = 0 get(ignore 'type'), > 0 set(use 'type')

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI element_list_query(PELEMENT_HEADER element);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI element_list_install(PELEMENT_HEADER element, PVOID resource_file_handle);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI element_list_initialize(PELEMENT_HEADER element);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI element_list_update(PELEMENT_HEADER element);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI element_list_generate(PELEMENT_HEADER element, GENERATE_PHASE phase);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI element_list_plot(PELEMENT_HEADER element);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI element_list_erase(PELEMENT_HEADER element);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI element_list_kill(PELEMENT_HEADER element);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI mouse_list_install(PMOUSERECT rect, PGAUGEHDR gauge_header, PPIXPOINT size);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI mouse_list_register(PMOUSERECT rect, PGAUGEHDR gauge_header);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI mouse_list_unregister(PMOUSERECT rect, PGAUGEHDR gauge_header);

	[[deprecated("Panels API is now deprecated")]]
	BOOL FSAPI panel_window_toggle(UINT32 panel_id);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	ERR FSAPI trigger_key_event(ID32 event_id, UINT32 value);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI register_var_by_name(PVOID var, VAR_TYPE var_type, PSTRINGZ name);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI initialize_var(PMODULE_VAR module_var);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI initialize_var_by_name(PMODULE_VAR module_var, PSTRINGZ name);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI lookup_var(PMODULE_VAR module_var);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI unregister_var_by_name(PSTRINGZ name);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI unregister_all_named_vars(void);

	[[deprecated("Panels API is now deprecated")]]
	BOOL FSAPI panel_window_close_ident(UINT32 panel_id);

	[[deprecated("Panels API is now deprecated")]]
	BOOL FSAPI panel_window_open_ident(UINT32 panel_id);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI panel_window_toggle_hud_color(void);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI panel_window_toggle_hud_units(void);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI radio_stack_popup(void);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI radio_stack_autoclose(void);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	ID FSAPI check_named_variable(PCSTRINGZ name);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	ID FSAPI register_named_variable(PCSTRINGZ name);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	FLOAT64 FSAPI get_named_variable_value(ID id);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	FLOAT64 FSAPI get_named_variable_typed_value(ID id, ENUM units);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI set_named_variable_value(ID id, FLOAT64 value);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI set_named_variable_typed_value(ID id, FLOAT64 value, ENUM units);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	PCSTRINGZ FSAPI get_name_of_named_variable(ID id);

	[[deprecated("Panels API is now deprecated")]]
	PCSTRINGZ FSAPI panel_resource_string_get(ID32 id);

	[[deprecated("Panels API is now deprecated")]]
	BOOL FSAPI panel_window_toggle_menu_id(ID32 menu_id);

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI element_use_color(PELEMENT_HEADER element, BOOL override, UINT32 color);  // color actually is  COLORREF

	[[deprecated("Panels API is now deprecated")]]
	void FSAPI set_gauge_flags(PCSTRINGZ name, FLAGS32 newflags);        // newlags is GAUGE_FLAG_NORMAL, GAUGE_FLAG_HIDDEN... 

	[[deprecated("Panels API is now deprecated")]]
	FLAGS32 FSAPI get_gauge_flags(PCSTRINGZ name);

	[[deprecated("Panels API is now deprecated")]]
	BOOL FSAPI gauge_calculator_code_precompile(PCSTRINGZ* pCompiled, UINT32* pCompiledSize, PCSTRINGZ source);

	[[deprecated("Panels API is now deprecated")]]
	BOOL FSAPI execute_calculator_code(PCSTRINGZ code, FLOAT64* fvalue, SINT32* ivalue, PCSTRINGZ* svalue);

	[[deprecated("Panels API is now deprecated")]]
	BOOL FSAPI format_calculator_string(PSTRINGZ result, UINT32 resultsize, PCSTRINGZ format);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	ENUM FSAPI get_units_enum(PCSTRINGZ unitname);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	ENUM FSAPI get_aircraft_var_enum(PCSTRINGZ simvar);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	FLOAT64 FSAPI aircraft_varget(ENUM simvar, ENUM units, SINT32 index);

	[[deprecated("Panels API is now deprecated")]]
	BOOL FSAPI panel_register_c_callback(PCSTRINGZ name, IPanelCCallback* pcallback);

	[[deprecated("Panels API is now deprecated")]]
	IPanelCCallback* FSAPI panel_get_registered_c_callback(PCSTRINGZ name);

	[[deprecated("Panels API is now deprecated")]]
	IAircraftCCallback* FSAPI panel_get_aircraft_c_callback(PCSTRINGZ name);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI send_key_event(ID32 event_id, UINT32 value);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI register_key_event_handler(GAUGE_KEY_EVENT_HANDLER handler, PVOID userdata);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI unregister_key_event_handler(GAUGE_KEY_EVENT_HANDLER handler, PVOID userdata);

	[[deprecated("Panels API is now deprecated")]]
	bool FSAPI process_shared_event_out(PGAUGEHDR gauge_header, BYTE* pBuf, UINT32 nSize);

	[[deprecated("Panels API is now deprecated")]]
	bool FSAPI is_master();
	
	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	ERR FSAPI trigger_key_event_EX1(ID32 event_id, UINT32 value0, UINT32 value1 = 0, UINT32 value2 = 0, UINT32 value3 = 0, UINT32 value4 = 0);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI register_key_event_handler_EX1(GAUGE_KEY_EVENT_HANDLER_EX1 handler, PVOID userdata);

	[[deprecated("Use MSFS_Event.h and MSFS_Vars.h instead")]]
	void FSAPI unregister_key_event_handler_EX1(GAUGE_KEY_EVENT_HANDLER_EX1 handler, PVOID userdata);
}
/*
// Module Export Structure
typedef struct  PANELS
{
	FSLINKAGE
	void        (FSAPI *reserved1) (void);
	void        (FSAPI *reserved2) (void);
	void        (FSAPI *reserved3) (void);
	void        (FSAPI *reserved4) (void);
	void        (FSAPI *reserved5) (void);
	BOOL        (FSAPI *is_panel_window_visible_ident) (UINT32 panel_id);
	ENUM        (FSAPI *tooltip_units_getset) (int action, ENUM type);  // action < 0 - toggle(ignore 'type'), = 0 get(ignore 'type'), > 0 set(use 'type')
	void        (FSAPI *reserved7) (void);
	void        (FSAPI *reserved8) (void);
	void        (FSAPI *reserved9) (void);
	void        (FSAPI *reserved10) (void);
	void        (FSAPI *reserved11) (void);
	void        (FSAPI *reserved12) (void);
	void        (FSAPI *reserved13) (void);
	void        (FSAPI *reserved14) (void);
	void        (FSAPI *reserved15) (void);
	void        (FSAPI *reserved16) (void);
	void        (FSAPI *reserved17) (void);
	void        (FSAPI *element_list_query) ( PELEMENT_HEADER element );
	void        (FSAPI *element_list_install) ( PELEMENT_HEADER element, PVOID resource_file_handle );
	void        (FSAPI *element_list_initialize) ( PELEMENT_HEADER element );
	void        (FSAPI *element_list_update) ( PELEMENT_HEADER element );
	void        (FSAPI *element_list_generate) ( PELEMENT_HEADER element, GENERATE_PHASE phase );
	void        (FSAPI *element_list_plot) ( PELEMENT_HEADER element );
	void        (FSAPI *element_list_erase) ( PELEMENT_HEADER element );
	void        (FSAPI *element_list_kill) ( PELEMENT_HEADER element );
	void        (FSAPI *mouse_list_install) ( PMOUSERECT rect, PGAUGEHDR gauge_header, PPIXPOINT size );
	void        (FSAPI *mouse_list_register) ( PMOUSERECT rect, PGAUGEHDR gauge_header );
	void        (FSAPI *mouse_list_unregister) ( PMOUSERECT rect, PGAUGEHDR gauge_header );
	BOOL        (FSAPI *panel_window_toggle) (UINT32 panel_id);
	ERR         (FSAPI *trigger_key_event) ( ID32 event_id, UINT32 value );
	void        (FSAPI *register_var_by_name) ( PVOID var, VAR_TYPE var_type, PSTRINGZ name );
	void        (FSAPI *initialize_var) ( PMODULE_VAR module_var );
	void        (FSAPI *initialize_var_by_name) ( PMODULE_VAR module_var, PSTRINGZ name );
	void        (FSAPI *lookup_var) ( PMODULE_VAR module_var );
	void        (FSAPI *unregister_var_by_name) ( PSTRINGZ name );
	void        (FSAPI *unregister_all_named_vars) (void);
	void        (FSAPI *reserved18) (void);
	void        (FSAPI *reserved19) (void);
	BOOL        (FSAPI *panel_window_close_ident) (UINT32 panel_id);
	BOOL        (FSAPI *panel_window_open_ident) (UINT32 panel_id);
	void        (FSAPI *panel_window_toggle_hud_color) (void);
	void        (FSAPI *panel_window_toggle_hud_units) (void);
	void        (FSAPI *radio_stack_popup) (void);
	void        (FSAPI *radio_stack_autoclose) (void);
	ID          (FSAPI *check_named_variable) (PCSTRINGZ name);
	ID          (FSAPI *register_named_variable) (PCSTRINGZ name);
	FLOAT64     (FSAPI *get_named_variable_value) (ID id);
	FLOAT64     (FSAPI *get_named_variable_typed_value) (ID id, ENUM units);
	void        (FSAPI *set_named_variable_value) (ID id, FLOAT64 value);
	void        (FSAPI *set_named_variable_typed_value) (ID id, FLOAT64 value, ENUM units);
	void        (FSAPI *reserved26) (void);
	void        (FSAPI *reserved27) (void);
	PCSTRINGZ   (FSAPI *get_name_of_named_variable) (ID id);
	void        (FSAPI *reserved29) (void);
	PCSTRINGZ   (FSAPI *panel_resource_string_get) (ID32 id);
	BOOL        (FSAPI *panel_window_toggle_menu_id) (ID32 menu_id);
	void        (FSAPI *reserved30) (void);
	void        (FSAPI *reserved31) (void);
	void        (FSAPI *element_use_color) (PELEMENT_HEADER element, BOOL override, UINT32 color);  // color actually is  COLORREF
	void        (FSAPI *set_gauge_flags) (PCSTRINGZ name, FLAGS32 newflags);        // newlags is GAUGE_FLAG_NORMAL, GAUGE_FLAG_HIDDEN...
	FLAGS32     (FSAPI *get_gauge_flags) (PCSTRINGZ name);
	BOOL        (FSAPI *gauge_calculator_code_precompile) (PCSTRINGZ* pCompiled, UINT32* pCompiledSize, PCSTRINGZ source);
	BOOL        (FSAPI *execute_calculator_code) (PCSTRINGZ code, FLOAT64* fvalue, SINT32* ivalue, PCSTRINGZ* svalue);
	BOOL        (FSAPI *format_calculator_string) (PSTRINGZ result, UINT32 resultsize, PCSTRINGZ format);
	BOOL        (FSAPI *reserved32) (void);
	BOOL        (FSAPI *reserved33) (void);
	ENUM        (FSAPI *get_units_enum) (PCSTRINGZ unitname);
	ENUM        (FSAPI *get_aircraft_var_enum) (PCSTRINGZ simvar);
	FLOAT64     (FSAPI *aircraft_varget) (ENUM simvar, ENUM units, SINT32 index);
	BOOL        (FSAPI *panel_register_c_callback) (PCSTRINGZ name, IPanelCCallback* pcallback);
	IPanelCCallback* (FSAPI *panel_get_registered_c_callback) (PCSTRINGZ name);
	IAircraftCCallback* (FSAPI *panel_get_aircraft_c_callback) (PCSTRINGZ name);
	void        (FSAPI *send_key_event) (ID32 event_id, UINT32 value);
	void        (FSAPI *register_key_event_handler) (GAUGE_KEY_EVENT_HANDLER handler, PVOID userdata);
	void        (FSAPI *unregister_key_event_handler) (GAUGE_KEY_EVENT_HANDLER handler, PVOID userdata);
	bool        (FSAPI *process_shared_event_out)(PGAUGEHDR gauge_header, BYTE* pBuf, UINT32 nSize);
	bool        (FSAPI *is_master)();
} PANELS, *PPANELS, **PPPANELS;

#define element_list_query  (ImportTable.PANELSentry.fnptr->element_list_query)
#define element_list_install    (ImportTable.PANELSentry.fnptr->element_list_install)
#define element_list_initialize (ImportTable.PANELSentry.fnptr->element_list_initialize)
#define element_list_update (ImportTable.PANELSentry.fnptr->element_list_update)
#define element_list_generate   (ImportTable.PANELSentry.fnptr->element_list_generate)
#define element_list_plot   (ImportTable.PANELSentry.fnptr->element_list_plot)
#define element_list_erase  (ImportTable.PANELSentry.fnptr->element_list_erase)
#define element_list_kill   (ImportTable.PANELSentry.fnptr->element_list_kill)
#define mouse_list_install  (ImportTable.PANELSentry.fnptr->mouse_list_install)
#define mouse_list_register (ImportTable.PANELSentry.fnptr->mouse_list_register)
#define mouse_list_unregister   (ImportTable.PANELSentry.fnptr->mouse_list_unregister)
#define panel_window_toggle (ImportTable.PANELSentry.fnptr->panel_window_toggle)
#define trigger_key_event   (ImportTable.PANELSentry.fnptr->trigger_key_event)
#define register_var_by_name    (ImportTable.PANELSentry.fnptr->register_var_by_name)
#define initialize_var  (ImportTable.PANELSentry.fnptr->initialize_var)
#define initialize_var_by_name  (ImportTable.PANELSentry.fnptr->initialize_var_by_name)
#define lookup_var  (ImportTable.PANELSentry.fnptr->lookup_var)
#define unregister_var_by_name  (ImportTable.PANELSentry.fnptr->unregister_var_by_name)
#define unregister_all_named_vars   (ImportTable.PANELSentry.fnptr->unregister_all_named_vars)
#define panel_window_close_ident    (ImportTable.PANELSentry.fnptr->panel_window_close_ident)
#define panel_window_open_ident (ImportTable.PANELSentry.fnptr->panel_window_open_ident)
#define is_panel_window_visible_ident   (ImportTable.PANELSentry.fnptr->is_panel_window_visible_ident)
#define panel_window_toggle_hud_color   (ImportTable.PANELSentry.fnptr->panel_window_toggle_hud_color)
#define panel_window_toggle_hud_units   (ImportTable.PANELSentry.fnptr->panel_window_toggle_hud_units)
#define radio_stack_popup   (ImportTable.PANELSentry.fnptr->radio_stack_popup)
#define radio_stack_autoclose   (ImportTable.PANELSentry.fnptr->radio_stack_autoclose)
#define panel_resource_string_get   (ImportTable.PANELSentry.fnptr->panel_resource_string_get)
#define panel_window_toggle_menu_id (ImportTable.PANELSentry.fnptr->panel_window_toggle_menu_id)
#define element_use_color   (ImportTable.PANELSentry.fnptr->element_use_color)
#define check_named_variable    (ImportTable.PANELSentry.fnptr->check_named_variable)
#define register_named_variable (ImportTable.PANELSentry.fnptr->register_named_variable)
#define get_named_variable_value    (ImportTable.PANELSentry.fnptr->get_named_variable_value)
#define get_named_variable_typed_value  (ImportTable.PANELSentry.fnptr->get_named_variable_typed_value)
#define set_named_variable_value    (ImportTable.PANELSentry.fnptr->set_named_variable_value)
#define set_named_variable_typed_value  (ImportTable.PANELSentry.fnptr->set_named_variable_typed_value)
#define get_name_of_named_variable  (ImportTable.PANELSentry.fnptr->get_name_of_named_variable)
#define set_gauge_flags (ImportTable.PANELSentry.fnptr->set_gauge_flags)
#define get_gauge_flags (ImportTable.PANELSentry.fnptr->get_gauge_flags)
#define gauge_calculator_code_precompile    (ImportTable.PANELSentry.fnptr->gauge_calculator_code_precompile)
#define execute_calculator_code (ImportTable.PANELSentry.fnptr->execute_calculator_code)
#define format_calculator_string    (ImportTable.PANELSentry.fnptr->format_calculator_string)
#define get_units_enum  (ImportTable.PANELSentry.fnptr->get_units_enum)
#define get_aircraft_var_enum   (ImportTable.PANELSentry.fnptr->get_aircraft_var_enum)
#define aircraft_varget (ImportTable.PANELSentry.fnptr->aircraft_varget)
#define panel_register_c_callback   (ImportTable.PANELSentry.fnptr->panel_register_c_callback)
#define panel_get_registered_c_callback (ImportTable.PANELSentry.fnptr->panel_get_registered_c_callback)
#define panel_get_aircraft_c_callback   (ImportTable.PANELSentry.fnptr->panel_get_aircraft_c_callback)
#define send_key_event  (ImportTable.PANELSentry.fnptr->send_key_event)
#define register_key_event_handler  (ImportTable.PANELSentry.fnptr->register_key_event_handler)
#define unregister_key_event_handler    (ImportTable.PANELSentry.fnptr->unregister_key_event_handler)
#define process_shared_event_out    ( ImportTable.PANELSentry.fnptr->process_shared_event_out)
#define is_master ( ImportTable.PANELSentry.fnptr->is_master)

// This is the gauge's import table definition
typedef struct GAUGESIMPORT
{
	struct
	{
		ID          fnID;
		PPANELS     fnptr;
	}PANELSentry;

	struct
	{
		ID          fnID;
		PVOID       fnptr;
	}nullentry;
} GAUGESIMPORT, *PGAUGESIMPORT;

EXTERN_C DECLSPEC_EXPORT GAUGESIMPORT ImportTable;

*/

// </ASOBO-MOD>



#if defined(_MSC_VER) 
#pragma warning( push )
#pragma warning( disable:4200 )
#endif

// This is the module's export table definition
typedef struct  GAUGESLINKAGE
{
	FSLINKAGE
		PGAUGEHDR       gauge_header_ptr[];
} GAUGESLINKAGE, *PGAUGESLINKAGE, **PPGAUGESLINKAGE;

#if defined(_MSC_VER) 
#pragma warning( pop )
#endif

EXTERN_C DECLSPEC_EXPORT    GAUGESLINKAGE   Linkage;



// Old gauge functions
static void FSAPI query_routine_610(void)
{
	element_list_query(*Linkage.gauge_header_ptr[0]->elements_list);
}

static void FSAPI install_routine_610(PVOID resource_file_handle)
{
	element_list_install(*Linkage.gauge_header_ptr[0]->elements_list, resource_file_handle);
}

static void FSAPI initialize_routine_610(void)
{
	element_list_initialize(*Linkage.gauge_header_ptr[0]->elements_list);
}

static void FSAPI update_routine_610(void)
{
	element_list_update(*Linkage.gauge_header_ptr[0]->elements_list);
}

static void FSAPI generate_routine_610(GENERATE_PHASE phase)
{
	element_list_generate(*Linkage.gauge_header_ptr[0]->elements_list, phase);
}

static void FSAPI draw_routine_610(void)
{
	element_list_plot(*Linkage.gauge_header_ptr[0]->elements_list);
}

static void FSAPI kill_routine_610(void)
{
	element_list_kill(*Linkage.gauge_header_ptr[0]->elements_list);
}


//-------------------------------------------------------------------------------
// GAUGE_TABLE_BEGIN
// GAUGE_TABLE_ENTRY
// GAUGE_TABLE_END
//
//  These macros build up the export table for the module by inserting gauge names
//  into the list of gauges
//-------------------------------------------------------------------------------
#define GAUGE_TABLE_BEGIN()                                     \
        extern  GAUGEHDR    gauge_header;                       \
        void FSAPI  module_init(void){}                         \
        void FSAPI  module_deinit(void){}                       \
        BOOL WINAPI DllMain (HINSTANCE hDLL, DWORD dwReason, LPVOID lpReserved) \
        {                                                       \
            return TRUE;                                        \
        }                                                       \
                                                                \
                                                                \
        /* This is the module's import table.   */              \
        GAUGESIMPORT    ImportTable =                           \
        {                                                       \
            { 0x0000000F, (PPANELS)NULL },                      \
            { 0x00000000, NULL }                                \
        };                                                      \
                                                                \
                                                                \
        /* This is the module's export table.   */              \
        GAUGESLINKAGE   Linkage =                               \
        {                                                       \
            0x00000013,                                         \
            module_init,                                        \
            module_deinit,                                      \
            0,                                                  \
            0,                                                  \
            FS9LINK_VERSION, {

#define GAUGE_TABLE_ENTRY(pgauge_header)        (pgauge_header),

#define GAUGE_TABLE_END()                       0 }};

#define GAUGE_HEADER_FS1000(            \
                gaugehdr_var_name,      \
                default_size_mm,        \
                gauge_name,             \
                element_list,           \
                pmouse_rect,            \
                pgauge_callback,        \
                user_data,              \
                usage,                  \
                guid,                   \
                serialize_size_callback,\
                serialize_callback,     \
    			deserialize_callback,   \
    			event_size_callback,    \
    			process_event_callback) \
    GAUGEHDR gaugehdr_var_name =        \
    {                                   \
        GAUGE_HEADER_VERSION_FS1000,    \
        gauge_name,                     \
        element_list,                   \
        query_routine_610,              \
        install_routine_610,            \
        initialize_routine_610,         \
        update_routine_610,             \
        generate_routine_610,           \
        draw_routine_610,               \
        kill_routine_610,               \
        NULL,                           \
        default_size_mm,                \
        0,                              \
        0.0,                            \
        0.0,                            \
        NULL,                           \
        NULL,                           \
        {0, 0},                         \
        NULL,                           \
        pmouse_rect,                    \
        pgauge_callback,                \
        user_data,                      \
        NULL,                           \
        usage,                          \
        0,                              \
        NULL,                           \
        {0, 0},                         \
        {0.0},                          \
        0,                              \
        NULL,                           \
        guid,                           \
        0,                              \
        serialize_size_callback,        \
        serialize_callback,             \
	    deserialize_callback,           \
	    event_size_callback,            \
	    process_event_callback,         \
    };

#define GAUGE_HEADER_FS900(             \
                gaugehdr_var_name,      \
                default_size_mm,        \
                gauge_name,             \
                element_list,           \
                pmouse_rect,            \
                pgauge_callback,        \
                user_data,              \
                usage)                  \
    GAUGEHDR gaugehdr_var_name =        \
    {                                   \
        GAUGE_HEADER_VERSION_FS900,      \
        gauge_name,                     \
        element_list,                   \
        query_routine_610,              \
        install_routine_610,            \
        initialize_routine_610,         \
        update_routine_610,             \
        generate_routine_610,           \
        draw_routine_610,               \
        kill_routine_610,               \
        NULL,                           \
        default_size_mm,                \
        0,                              \
        0.0,                            \
        0.0,                            \
        NULL,                           \
        NULL,                           \
        {0, 0},                         \
        NULL,                           \
        pmouse_rect,                    \
        pgauge_callback,                \
        user_data,                      \
        NULL,                           \
        usage,                          \
        0,                              \
        NULL,                           \
        {0, 0},                         \
        {0.0},                          \
        0,                              \
        NULL,                           \
    };


#define GAUGE_HEADER_FS800(             \
                gaugehdr_var_name,      \
                default_size_mm,        \
                gauge_name,             \
                element_list,           \
                pmouse_rect,            \
                pgauge_callback,        \
                user_data,              \
                usage)                  \
    GAUGEHDR gaugehdr_var_name =        \
    {                                   \
        GAUGE_HEADER_VERSION_FS800,     \
        gauge_name,                     \
        element_list,                   \
        query_routine_610,              \
        install_routine_610,            \
        initialize_routine_610,         \
        update_routine_610,             \
        generate_routine_610,           \
        draw_routine_610,               \
        kill_routine_610,               \
        NULL,                           \
        default_size_mm,                \
        0,                              \
        0.0,                            \
        0.0,                            \
        NULL,                           \
        NULL,                           \
        {0, 0},                         \
        NULL,                           \
        pmouse_rect,                    \
        pgauge_callback,                \
        user_data,                      \
        NULL,                           \
        usage,                          \
        0,                              \
        NULL,                           \
        {0, 0},                         \
        {0.0},                          \
    };

#define GAUGE_HEADER_FS700(             \
                default_size_mm,        \
                gauge_name,             \
                element_list,           \
                pmouse_rect,            \
                pgauge_callback,        \
                user_data,              \
                parameters,             \
                usage)                  \
        GAUGE_HEADER_FS800(             \
                GAUGEHDR_VAR_NAME,      \
                default_size_mm,        \
                gauge_name,             \
                element_list,           \
                pmouse_rect,            \
                pgauge_callback,        \
                user_data,              \
                usage)



#pragma pack(pop)

typedef bool(*PFNGAUGECALLBACK)(FsContext ctx, int service_id, void* pData);

#endif
