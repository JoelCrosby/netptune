import { Basemodel } from './basemodel';

export interface Board extends Basemodel {
  name: string;
  identifier: string;
  projectId: number;
}
