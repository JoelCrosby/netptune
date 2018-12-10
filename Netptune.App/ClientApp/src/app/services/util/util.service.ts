import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UtilService {

  constructor() {
  }

  smoothUpdate(sourceArray: any, newArray: any): void {

    if (!(sourceArray instanceof Array)) {
      throw new Error('sourceArray must be of type array!');
    }
    if (!(newArray instanceof Array)) {
      throw new Error('newArray must be of type array!');
    }

    sourceArray.splice(0, sourceArray.length);
    sourceArray.push(...newArray);
  }

  deepClone(objectToClone: any): any {
    return JSON.parse(JSON.stringify(objectToClone));
  }

}
