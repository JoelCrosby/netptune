"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var Project = /** @class */ (function () {
    function Project(projectTypeService) {
        if (projectTypeService === void 0) { projectTypeService = null; }
        this.projectTypeService = projectTypeService;
    }
    Project.prototype.markDirty = function () {
        this.isDirty = true;
    };
    return Project;
}());
exports.Project = Project;
//# sourceMappingURL=project.js.map