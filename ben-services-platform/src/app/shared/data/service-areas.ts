export const SERVICE_AREAS_BY_STATE: Record<string, string[]> = {
  DC: ['Washington DC', 'Georgetown', 'Capitol Hill', 'NoMa', 'Navy Yard', 'Shaw', 'K Street'],
  VA: [
    'Arlington',
    'Alexandria',
    'Fairfax',
    'Reston',
    'Herndon',
    'Tysons',
    'Ballston',
    'Clarendon',
    'Courthouse',
    'Crystal City',
    'Pentagon City',
    'Rosslyn',
    'Shirlington',
    'Columbia Pike',
    'Cherrydale',
    'Lyon Village',
    'Bluemont',
    'Westover',
    'Fairlington'
  ],
  MD: [
    'Bethesda',
    'Silver Spring',
    'Rockville',
    'College Park',
    'Columbia',
    'Baltimore',
    'Towson',
    'Dundalk',
    'Essex',
    'Catonsville',
    'Pikesville',
    'Parkville',
    'Rosedale',
    'White Marsh',
    'Owings Mills',
    'Glen Burnie',
    'Linthicum Heights',
    'Ellicott City'
  ],
  NY: ['Manhattan', 'Brooklyn', 'Queens', 'Bronx', 'Staten Island', 'Midtown', 'Downtown']
};

export const SERVICE_AREA_STATES = ['DC', 'VA', 'MD', 'NY'] as const;

export type ServiceAreaState = (typeof SERVICE_AREA_STATES)[number];
