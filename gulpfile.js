import gulp from 'gulp';
import msbuild from 'gulp-msbuild';
import debug from 'gulp-debug';
import foreach from 'gulp-foreach';
import gulpConfig from './gulp-config.js';

var publishProjects = function (location, dest) {
    var targets = ['Build'];

    console.log('Publish to ' + dest + ' folder');

    return gulp.src([location + '\\**\\*.csproj'])
        .pipe(foreach(function (stream, file) {
            return stream
                .pipe(debug({ title: 'Building project:' }))
                .pipe(msbuild({
                    targets: targets,
                    configuration: gulpConfig.buildConfiguration,
                    logCommand: false,
                    verbosity: 'minimal',
                    stdout: true,
                    errorOnFail: true,
                    toolsVersion: 'auto',
                    properties: {
                        DeployOnBuild: 'true',
                        DeployDefaultTarget: 'WebPublish',
                        WebPublishMethod: 'FileSystem',
                        DeleteExistingFiles: 'false',
                        publishUrl: dest,
                        _FindDependencies: 'false'
                    }
                })
            );
        })
    );
};

gulp.task('Build-Solution', function () {
    var targets = ['Build'];

    return gulp.src('.\\' + gulpConfig.solutionName + '.sln').pipe(
        msbuild({
            targets: targets,
            configuration: gulpConfig.buildconfiguration,
            logCommand: false,
            verbosity: 'minimal',
            stdout: true,
            errorOnFail: true,
            toolsVersion: 'auto'
        })
    );
});

gulp.task('Publish-Foundation-Projects', function () {
    return publishProjects('.\\src\\Foundation', gulpConfig.webRoot);
});

gulp.task('Publish-Feature-Projects', function () {
    return publishProjects('.\\src\\Feature', gulpConfig.webRoot);
});

gulp.task('Publish-Project-Projects', function () {
    return publishProjects('.\\src\\Project', gulpConfig.webRoot);
});

gulp.task('default', gulp.series(
        'Build-Solution',
        'Publish-Foundation-Projects',
        'Publish-Feature-Projects',
        'Publish-Project-Projects'
    )
);