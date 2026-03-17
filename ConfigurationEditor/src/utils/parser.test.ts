import assert from 'node:assert/strict';
import { formatToOriginal, parseParserConfig } from './parser';

function testParserUsesPhysicalLineNumbersForRoundTrip() {
  const content = [
    '# top',
    '',
    '1.00|ScopesModule.TypeA|10|NodeA',
    '# between',
    '2.00|LoopsModule.TypeB|20|NodeB',
  ].join('\n');

  const parsed = parseParserConfig(content);

  assert.equal(parsed.rows[0].lineNumber, 3);
  assert.equal(parsed.rows[1].lineNumber, 5);

  const formatted = formatToOriginal(parsed);
  assert.equal(
    formatted,
    [
      '# top',
      '1.00|ScopesModule.TypeA|10|NodeA',
      '# between',
      '2.00|LoopsModule.TypeB|20|NodeB',
    ].join('\n'),
  );
}

function testFormatHandlesSerializedCommentsMapCoordinates() {
  const content = [
    '# top',
    '1.00|ScopesModule.TypeA|10|NodeA',
    '# bottom',
  ].join('\n');

  const parsed = parseParserConfig(content);

  const serializedLikeConfig = JSON.parse(JSON.stringify(parsed)) as unknown as typeof parsed;
  (serializedLikeConfig as unknown as { comments: Record<number, string> }).comments = {
    1: '# top',
    3: '# bottom',
  };

  assert.equal(
    formatToOriginal(serializedLikeConfig),
    [
      '# top',
      '1.00|ScopesModule.TypeA|10|NodeA',
      '# bottom',
    ].join('\n'),
  );
}

testParserUsesPhysicalLineNumbersForRoundTrip();
testFormatHandlesSerializedCommentsMapCoordinates();
console.log('parser.test.ts passed');
